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
done < <(find src tests -name '*.vbproj' | sort)

# 5. Brave must not touch shared projects without Sigma: enforced by CODEOWNERS, listed here for visibility.
[ -f .github/CODEOWNERS ] || err "CODEOWNERS missing"

[ "$fail" -eq 0 ] && echo "All guards passed."
exit "$fail"
