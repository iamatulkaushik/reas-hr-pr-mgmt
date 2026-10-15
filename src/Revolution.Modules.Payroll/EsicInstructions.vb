''' <summary>Second sheet of the ESIC workbook. Static text only. The portal reads sheet 1.</summary>
Public NotInheritable Class EsicInstructions
    Public Const SheetName As String = "Instructions & Reason Codes"

    Private Sub New()
    End Sub

    ''' <summary>Reason code table and fill-in instructions, as in the ESIC template. Steps 10a and 10b (convert to Text by hand) are left out: the exporter writes Text.</summary>
    Public Shared Function Table() As IReadOnlyList(Of String())
        Dim rows As New List(Of String())()
        rows.Add({"Reason", "Code", "Note"})
        rows.Add({"Without Reason", "0", "Leave last working day as blank"})
        rows.Add({"On Leave", "1", "Leave last working day as blank"})
        rows.Add({"Left Service", "2", "Please provide last working day (dd/mm/yyyy). IP will not appear from next wage period"})
        rows.Add({"Retired", "3", "Please provide last working day (dd/mm/yyyy). IP will not appear from next wage period"})
        rows.Add({"Out of Coverage", "4", "Please provide last working day (dd/mm/yyyy). IP will not appear from next contribution period. This option is valid only if Wage Period is April/October. In case any other month then IP will continue to appear in the list"})
        rows.Add({"Expired", "5", "Please provide last working day (dd/mm/yyyy). IP will not appear from next wage period"})
        rows.Add({"Non Implemented area", "6", "Please provide last working day (dd/mm/yyyy)."})
        rows.Add({"Compliance by Immediate Employer", "7", "Leave last working day as blank"})
        rows.Add({"Suspension of work", "8", "Leave last working day as blank"})
        rows.Add({"Strike/Lockout", "9", "Leave last working day as blank"})
        rows.Add({"Retrenchment", "10", "Please provide last working day (dd/mm/yyyy). IP will not appear from next wage period"})
        rows.Add({"No Work", "11", "Leave last working day as blank"})
        rows.Add({"Doesnt Belong To This Employer", "12", "Leave last working day as blank"})
        rows.Add({"Duplicate IP", "13", "Leave last working day as blank"})
        rows.Add({String.Empty})
        rows.Add({"Instructions to fill in the excel file:"})
        rows.Add({"1. Enter the IP number, IP name, No. of Days, Total Monthly Wages, Reason for 0 wages(If Wages '0') & Last Working Day( only if employee has left service, Retired, Out of coverage, Expired, Non-Implemented area or Retrenchment. For other reasons, last working day must be left BLANK)."})
        rows.Add({"2. Number of days must me a whole number. Fractions should be rounded up to next higher whole number/integer"})
        rows.Add({"3. Excel sheet upload will lead to successful transaction only when all the Employees' (who are currently mapped in the system) details are entered perfectly in the excel sheet"})
        rows.Add({"4. Reasons are to be assigned numeric code and date has to be provided as mentioned in the table above"})
        rows.Add({"5. Once 0 wages given and last working day is mentioned as in reason codes (2,3,4,5,10) IP will be removed from the employer's record. Subsequent months will not have this IP listed under the employer. Last working day should be mentioned only if 'Number of days wages paid/payable' is '0'."})
        rows.Add({"6. In case IP has worked for part of the month(i.e. atleast 1 day wage is paid/payable) and left in between of the month, then last working day shouldn't be mentioned."})
        rows.Add({"7. Calculations – IP Contribution and Employer contribution calculation will be automatically done by the system"})
        rows.Add({"8. Date column format is dd/mm/yyyy or dd-mm-yyyy. Pad single digit dates with 0. Eg:- 2/5/2010 or 2-May-2010 is NOT acceptable. Correct format is 02/05/2010 or 02-05-2010"})
        rows.Add({"9. Excel file should be saved in .xls format (Excel 97-2003)"})
        rows.Add({"10. Note that all the column including date column should be in 'Text' format"})
        rows.Add({"11. If problem continues while upload, download a fresh template by clicking 'Sample MC Excel Template'. Then copy the data area from Step 8a.a – eg: copy Cell A2 to F8 (if there is data in 8 rows); Paste it in cell A2 in the fresh template. Upload it"})
        Return rows
    End Function
End Class
