''' <summary>Verhoeff check used by Aadhaar numbers. Detects every single-digit error and adjacent swaps.</summary>
Friend NotInheritable Class VerhoeffChecksum
    Private Sub New()
    End Sub

    Private Shared ReadOnly _multiplication As Integer(,) = {
        {0, 1, 2, 3, 4, 5, 6, 7, 8, 9},
        {1, 2, 3, 4, 0, 6, 7, 8, 9, 5},
        {2, 3, 4, 0, 1, 7, 8, 9, 5, 6},
        {3, 4, 0, 1, 2, 8, 9, 5, 6, 7},
        {4, 0, 1, 2, 3, 9, 5, 6, 7, 8},
        {5, 9, 8, 7, 6, 0, 4, 3, 2, 1},
        {6, 5, 9, 8, 7, 1, 0, 4, 3, 2},
        {7, 6, 5, 9, 8, 2, 1, 0, 4, 3},
        {8, 7, 6, 5, 9, 3, 2, 1, 0, 4},
        {9, 8, 7, 6, 5, 4, 3, 2, 1, 0}}

    Private Shared ReadOnly _permutation As Integer(,) = {
        {0, 1, 2, 3, 4, 5, 6, 7, 8, 9},
        {1, 5, 7, 6, 2, 8, 3, 0, 9, 4},
        {5, 8, 0, 3, 7, 9, 6, 1, 4, 2},
        {8, 9, 1, 6, 0, 4, 3, 5, 2, 7},
        {9, 4, 5, 3, 1, 2, 6, 8, 7, 0},
        {4, 2, 8, 6, 5, 7, 3, 9, 0, 1},
        {2, 7, 9, 3, 8, 0, 6, 4, 1, 5},
        {7, 0, 4, 6, 9, 1, 3, 2, 5, 8}}

    Private Const PermutationRows As Integer = 8

    ''' <summary>True when the digit string, including its last check digit, passes the Verhoeff test.</summary>
    Friend Shared Function IsValid(digits As String) As Boolean
        Dim c As Integer = 0
        Dim last As Integer = digits.Length - 1
        For i As Integer = 0 To last
            Dim digit As Integer = AscW(digits(last - i)) - AscW("0"c)
            c = _multiplication(c, _permutation(i Mod PermutationRows, digit))
        Next
        Return c = 0
    End Function
End Class
