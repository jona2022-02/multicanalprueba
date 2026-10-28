Imports System.Web.Optimization

Public Class Global_asax
    Inherits HttpApplication

    Sub Application_Start(sender As Object, e As EventArgs)
        ' Se desencadena al iniciar la aplicación
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
        AppLogger.Info("multicanalprueba iniciado.")
    End Sub

    Sub Application_Error(sender As Object, e As EventArgs)
        Dim ex = Server.GetLastError()
        AppLogger.LogError("Error no controlado en la aplicación.", ex)
    End Sub
End Class