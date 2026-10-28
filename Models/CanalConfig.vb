''' <summary>Configuración por canal (desde SQL). No guardar ApiKey en ViewState.</summary>
Public NotInheritable Class CanalConfig
    Public Property Canal As String
    Public Property UrlServicio As String
    Public Property ApiKey As String
    Public Property MostrarMenu As Boolean
    Public Property PaginaDestino As String
    Public Property TimeoutSegundos As Integer
End Class
