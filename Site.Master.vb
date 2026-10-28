Public Class SiteMaster
    Inherits MasterPage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
    End Sub

    ''' <summary>
    ''' Aplica visibilidad del menú según configuración del canal (sin If por canal en páginas).
    ''' </summary>
    Public Sub AplicarMenu(config As CanalConfig)
        If config Is Nothing OrElse Not config.MostrarMenu Then
            pnlMenuCanal.Visible = False
            Return
        End If

        pnlMenuCanal.Visible = True
        lnkEmpresas.NavigateUrl = "~/Empresas?canal=" & HttpUtility.UrlEncode(config.Canal)
    End Sub
End Class
