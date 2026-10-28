Imports System.Data
Imports System.Data.SqlClient

Public Class CanalConfigRepository
    Private ReadOnly _connectionString As String

    Public Sub New()
        Me.New(ConfigurationManager.ConnectionStrings("Multicanal").ConnectionString)
    End Sub

    Public Sub New(connectionString As String)
        If String.IsNullOrWhiteSpace(connectionString) Then
            Throw New ArgumentException("Connection string Multicanal no configurada.", "connectionString")
        End If
        _connectionString = connectionString
    End Sub

    Public Function ObtenerPorCanal(canal As String) As CanalConfig
        If String.IsNullOrWhiteSpace(canal) Then
            Return Nothing
        End If

        Using connection As New SqlConnection(_connectionString)
            Using command As New SqlCommand("dbo.usp_ConfiguracionCanal_Obtener", connection)
                command.CommandType = CommandType.StoredProcedure
                command.Parameters.Add("@Canal", SqlDbType.VarChar, 10).Value = canal.Trim().ToUpperInvariant()
                connection.Open()
                Using reader = command.ExecuteReader(CommandBehavior.SingleRow)
                    If Not reader.Read() Then
                        Return Nothing
                    End If
                    Return New CanalConfig With {
                        .Canal = Convert.ToString(reader("Canal")),
                        .UrlServicio = Convert.ToString(reader("UrlServicio")),
                        .ApiKey = Convert.ToString(reader("ApiKey")),
                        .MostrarMenu = Convert.ToBoolean(reader("MostrarMenu")),
                        .PaginaDestino = Convert.ToString(reader("PaginaDestino")),
                        .TimeoutSegundos = Convert.ToInt32(reader("TimeoutSegundos"))
                    }
                End Using
            End Using
        End Using
    End Function
End Class
