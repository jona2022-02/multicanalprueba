Imports System.Threading.Tasks

Public Class Empresas
    Inherits Page

    Private ReadOnly _configRepo As New CanalConfigRepository()
    Private ReadOnly _apiClient As New EmpresasApiClient()
    Private _configActual As CanalConfig

    Private Property EmpresasCargadas As List(Of EmpresaPago)
        Get
            Return TryCast(ViewState("EmpresasCargadas"), List(Of EmpresaPago))
        End Get
        Set(value As List(Of EmpresaPago))
            ViewState("EmpresasCargadas") = value
        End Set
    End Property

    Private Property CanalActual As String
        Get
            Return Convert.ToString(ViewState("CanalActual"))
        End Get
        Set(value As String)
            ViewState("CanalActual") = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim canal = Trim(If(Request.QueryString("canal"), String.Empty))
        litCanal.Text = If(String.IsNullOrEmpty(canal), "(no indicado)", Server.HtmlEncode(canal.ToUpperInvariant()))

        If String.IsNullOrWhiteSpace(canal) Then
            MostrarError("Indique el canal en la URL, por ejemplo Empresas?canal=AB")
            AplicarMenuMaster(Nothing)
            Return
        End If

        If IsPostBack Then
            Try
                _configActual = _configRepo.ObtenerPorCanal(If(CanalActual, canal))
            Catch ex As Exception
                AppLogger.LogError("Error al releer config en postback. Canal=" & canal, ex)
            End Try
            AplicarMenuMaster(_configActual)
            Return
        End If

        If Not PrepararConfiguracion(canal) Then
            Return
        End If

        RegisterAsyncTask(New PageAsyncTask(AddressOf CargarEmpresasAsync))
    End Sub

    Private Function PrepararConfiguracion(canal As String) As Boolean
        Try
            _configActual = _configRepo.ObtenerPorCanal(canal)
        Catch ex As Exception
            AppLogger.LogError("Error al leer configuración del canal '" & canal & "'.", ex)
            MostrarError("No fue posible obtener la configuración del canal. Intente más tarde.")
            AplicarMenuMaster(Nothing)
            Return False
        End Try

        If _configActual Is Nothing Then
            MostrarError("El canal '" & canal.ToUpperInvariant() & "' no existe o está inactivo.")
            AplicarMenuMaster(Nothing)
            Return False
        End If

        CanalActual = _configActual.Canal
        Session(SessionKeys.CanalActual) = _configActual.Canal
        AplicarMenuMaster(_configActual)
        Return True
    End Function

    Private Async Function CargarEmpresasAsync() As Task
        If _configActual Is Nothing Then Return

        Try
            Dim empresas As IList(Of EmpresaPago) = Await _apiClient.ObtenerEmpresasAsync(
                _configActual.UrlServicio,
                _configActual.ApiKey,
                _configActual.Canal,
                _configActual.TimeoutSegundos)

            Dim lista As New List(Of EmpresaPago)(empresas)
            EmpresasCargadas = lista
            rptEmpresas.DataSource = lista
            rptEmpresas.DataBind()
        Catch ex As EmpresasApiException
            AppLogger.LogError("Fallo al consumir servicio. Canal=" & _configActual.Canal & "; Status=" & Convert.ToString(ex.StatusCode), ex)
            MostrarError(MensajeUsuarioPara(ex))
        Catch ex As Exception
            AppLogger.LogError("Error inesperado al listar empresas. Canal=" & _configActual.Canal, ex)
            MostrarError("Ocurrió un problema al cargar las empresas. Intente más tarde.")
        End Try
    End Function

    Protected Sub rptEmpresas_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName <> "Seleccionar" Then Return

        Dim canal = If(CanalActual, Trim(If(Request.QueryString("canal"), String.Empty)))
        Dim config = If(_configActual, SafeObtenerConfig(canal))
        If config Is Nothing Then
            MostrarError("La configuración del canal ya no está disponible. Vuelva a cargar la página.")
            Return
        End If

        Dim empresaId = Convert.ToString(e.CommandArgument)
        Dim empresa = EmpresasCargadas?.FirstOrDefault(Function(x) String.Equals(x.Id, empresaId, StringComparison.Ordinal))
        If empresa Is Nothing Then
            MostrarError("La empresa seleccionada no es válida.")
            Return
        End If

        Session(SessionKeys.EmpresaSeleccionada) = empresa
        Session(SessionKeys.CanalActual) = config.Canal

        Dim destino = If(String.IsNullOrWhiteSpace(config.PaginaDestino), "Detalle.aspx", config.PaginaDestino)
        Response.Redirect("~/" & destino.TrimStart("/"c).Replace(".aspx", "") & "?canal=" & HttpUtility.UrlEncode(config.Canal), False)
        Context.ApplicationInstance.CompleteRequest()
    End Sub

    Private Function SafeObtenerConfig(canal As String) As CanalConfig
        Try
            Return _configRepo.ObtenerPorCanal(canal)
        Catch ex As Exception
            AppLogger.LogError("Error al obtener config en selección. Canal=" & canal, ex)
            Return Nothing
        End Try
    End Function

    Private Sub AplicarMenuMaster(config As CanalConfig)
        Dim masterPage = TryCast(Master, SiteMaster)
        masterPage?.AplicarMenu(config)
    End Sub

    Private Sub MostrarError(mensaje As String)
        pnlError.Visible = True
        litError.Text = Server.HtmlEncode(mensaje)
        rptEmpresas.Visible = False
    End Sub

    Private Shared Function MensajeUsuarioPara(ex As EmpresasApiException) As String
        If ex.StatusCode.HasValue AndAlso ex.StatusCode.Value = 401 Then
            Return "No se pudo autenticar con el servicio de empresas. Verifique la configuración del canal."
        End If
        If ex.Message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return "El servicio de empresas no respondió a tiempo. Intente nuevamente."
        End If
        Return "No se pudieron obtener las empresas en este momento."
    End Function
End Class
