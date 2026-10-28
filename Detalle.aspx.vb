Public Class Detalle
    Inherits Page

    Private ReadOnly _configRepo As New CanalConfigRepository()

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim canal = Trim(If(Request.QueryString("canal"), Convert.ToString(Session(SessionKeys.CanalActual))))
        Dim listadoUrl = "~/Empresas?canal=" & HttpUtility.UrlEncode(If(canal, String.Empty))

        Dim empresa = TryCast(Session(SessionKeys.EmpresaSeleccionada), EmpresaPago)
        If empresa Is Nothing OrElse String.IsNullOrWhiteSpace(empresa.Id) Then
            Response.Redirect(listadoUrl, False)
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If

        Dim config As CanalConfig = Nothing
        If Not String.IsNullOrWhiteSpace(canal) Then
            Try
                config = _configRepo.ObtenerPorCanal(canal)
            Catch ex As Exception
                AppLogger.LogError("Error al leer config en Detalle. Canal=" & canal, ex)
            End Try
        End If

        Dim masterPage = TryCast(Master, SiteMaster)
        masterPage?.AplicarMenu(config)

        litNombre.Text = Server.HtmlEncode(empresa.Nombre)
        litId.Text = Server.HtmlEncode(empresa.Id)
        litCanal.Text = Server.HtmlEncode(If(canal, String.Empty))
        imgLogo.ImageUrl = If(String.IsNullOrWhiteSpace(empresa.LogoUrl), String.Empty, empresa.LogoUrl)
        imgLogo.AlternateText = empresa.Nombre
        lnkVolver.NavigateUrl = listadoUrl
    End Sub
End Class
