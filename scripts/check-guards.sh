#!/usr/bin/env bash
# Repo drift guards. Fails the build on the problems that broke the earlier attempts.
set -u
fail=0
err() { echo "GUARD FAIL: $*" >&2; fail=1; }

# 1. SQL lives only in database/migrations.
while IFS= read -r f; do
  case "$f" in database/migrations/*) ;; *) err "SQL outside database/migrations: $f" ;; esac
done < <(find . -name '*.sql' -not -path './.git/*' -not -path '*/bin/*' -not -path '*/obj/*' | sed 's#^\./##')

# 2. Migration names: 4 digits, underscore, lowercase name; numbers unique.
dups=$(ls database/migrations 2>/dev/null | grep -E '\.sql$' | cut -c1-4 | sort | uniq -d)
[ -n "$dups" ] && err "Duplicate migration number(s): $dups"
for f in database/migrations/*.sql; do
  [ -e "$f" ] || continue
  b=$(basename "$f")
  echo "$b" | grep -Eq '^[0-9]{4}_[a-z0-9_]+\.sql$' || err "Bad migration name: $b"
done

# 3. An interface name is defined once, solution-wide.
dupi=$(grep -rhoE --include='*.vb' '^\s*(Public|Friend)\s+Interface\s+I[A-Za-z0-9_]+' src tests 2>/dev/null \
  | awk '{print $NF}' | sort | uniq -d)
[ -n "$dupi" ] && err "Interface defined more than once: $dupi"

# 4. Every project on disk is in the solution.
while IFS= read -r p; do
  grep -q "$p" Revolution.slnx || err "Project not in Revolution.slnx: $p"
done < <(find src tests tools -name '*.vbproj' | sort)

# 4b. Patch files are hand-over files, never part of the repository.
patches=$(git ls-files '*.patch' 2>/dev/null)
[ -n "$patches" ] && err "Patch file(s) committed: $patches"

# 4c. VB ignores case, so in a form a loop variable called text, top, name, size ... is the inherited property and does not compile (BC30039).
risky=$(grep -rnEi --include='*.vb' 'For Each[[:space:]]+(text|top|left|right|bottom|width|height|size|location|name|tag|font|parent|visible|enabled|controls|region|handle|site|container|cursor|opacity|padding|margin|anchor|dock|bounds|owner|icon|modal|focused)[[:space:]]+In[[:space:]]' src/Revolution.UI.Common src/Revolution.App src/Revolution.Modules.*.UI 2>/dev/null)
[ -n "$risky" ] && err "For Each variable named like a Control property. Give it another name and a type (For Each heading As String In ...): $risky"

# 4d. Anywhere: a For Each variable without a type that is named like a System namespace (text, io, linq, data, net ...) binds to the namespace and
# does not compile (BC30112). Give the variable another name, or write its type.
nsrisky=$(grep -rnEi --include='*.vb' 'For Each[[:space:]]+(text|io|linq|collections|net|data|xml|security|threading|diagnostics|globalization|reflection|runtime|buffers|numerics|resources|timers|transactions|formats|drawing)[[:space:]]+In[[:space:]]' src tests 2>/dev/null)
[ -n "$nsrisky" ] && err "For Each variable named like a System namespace (BC30112). Give it another name: $nsrisky"

# 5. Brave must not touch shared projects without Sigma: enforced by CODEOWNERS, listed here for visibility.
[ -f .github/CODEOWNERS ] || err "CODEOWNERS missing"

# 6. Every table with a CompanyID column joins a tenant security policy (ADR 0007). mst.Company, sec.[User] and sec.UserCompany are exempt (read before a company is chosen).
tables=$(awk '
  /CREATE TABLE/ { name=$0; sub(/.*CREATE TABLE[ \t]+/, "", name); sub(/[ \t\r(].*/, "", name); intable=1; has=0; next }
  intable && /CompanyID/ { has=1 }
  intable && /^[ \t]*\);/ { if (has) print name; intable=0 }
' database/migrations/*.sql 2>/dev/null | sort -u)
for t in $tables; do
  case "$t" in mst.Company|'sec.[User]'|sec.UserCompany) continue ;; esac
  grep -h "FILTER PREDICATE" database/migrations/*.sql 2>/dev/null | grep -qi "ON $t\b" || err "Table with CompanyID has no tenant security policy: $t"
done

# 7. A table or a security policy is created in one script only. A second CREATE TABLE is skipped by the IF OBJECT_ID guard and hides a different shape.
duptab=$(grep -rhoiE 'CREATE TABLE[[:space:]]+[A-Za-z]+\.\[?[A-Za-z_]+\]?' database/migrations 2>/dev/null | tr -d '\r' | sed -E 's/.*TABLE[[:space:]]+//I; s/[][]//g' | tr 'A-Z' 'a-z' | sort | uniq -d)
[ -n "$duptab" ] && err "Table created in more than one migration: $(echo $duptab)"
duppol=$(grep -rhoiE 'CREATE SECURITY POLICY[[:space:]]+[A-Za-z]+\.[A-Za-z_]+' database/migrations 2>/dev/null | tr -d '\r' | sed -E 's/.*POLICY[[:space:]]+//I' | tr 'A-Z' 'a-z' | sort | uniq -d)
[ -n "$duppol" ] && err "Security policy created in more than one migration: $(echo $duppol)"

# 8. A procedure that opens a transaction sets XACT_ABORT ON inside its own body (the setup session's SET does not reach the app connection).
# The latest definition of a procedure wins, because CREATE OR ALTER in a later script replaces it.
badtran=$(awk '
  BEGIN { IGNORECASE = 1 }
  FNR == 1 { inblk = 0 }
  /CREATE[ \t]+(OR[ \t]+ALTER[ \t]+)?PROCEDURE/ {
    name = $0; sub(/.*PROCEDURE[ \t]+/, "", name); sub(/[ \t\r(@].*/, "", name); name = tolower(name)
    inblk = 1; tran[name] = 0; abort[name] = 0; cur = name; next }
  /^GO[ \t\r]*$/ { inblk = 0; next }
  inblk && /BEGIN[ \t]+TRAN/ { tran[cur] = 1 }
  inblk && /SET[ \t]+XACT_ABORT[ \t]+ON/ { abort[cur] = 1 }
  END { for (n in tran) if (tran[n] && !abort[n]) print n }
' database/migrations/*.sql 2>/dev/null | sort)
[ -n "$badtran" ] && err "Procedure opens a transaction without SET XACT_ABORT ON: $(echo $badtran)"

[ "$fail" -eq 0 ] && echo "All guards passed."
exit "$fail"
