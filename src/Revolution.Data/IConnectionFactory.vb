Imports Microsoft.Data.SqlClient

''' <summary>Opens SQL connections. Repositories never build connection strings themselves.</summary>
Public Interface IConnectionFactory
    ''' <summary>Opens a connection with the tenant context already applied. The caller disposes it.</summary>
    Function Open() As SqlConnection
End Interface
