Imports System.IO

Public NotInheritable Class AppLogger
    Private Shared ReadOnly SyncRoot As New Object()

    Private Sub New()
    End Sub

    Public Shared Sub Info(mensaje As String)
        Write("INFO", mensaje, Nothing)
    End Sub

    Public Shared Sub LogError(mensaje As String, Optional ex As Exception = Nothing)
        Write("ERROR", mensaje, ex)
    End Sub

    Private Shared Sub Write(nivel As String, mensaje As String, ex As Exception)
        Try
            Dim folder = ResolveLogFolder()
            If Not Directory.Exists(folder) Then
                Directory.CreateDirectory(folder)
            End If
            Dim logFile = Path.Combine(folder, $"multicanal-{DateTime.UtcNow:yyyyMMdd}.log")
            Dim detalle = If(ex Is Nothing, String.Empty, Environment.NewLine & ex.ToString())
            Dim linea = $"{DateTime.UtcNow:o} [{nivel}] {mensaje}{detalle}{Environment.NewLine}"
            SyncLock SyncRoot
                File.AppendAllText(logFile, linea)
            End SyncLock
        Catch
        End Try
    End Sub

    Private Shared Function ResolveLogFolder() As String
        Dim ctx = HttpContext.Current
        If ctx IsNot Nothing Then
            Return ctx.Server.MapPath("~/App_Data/Logs")
        End If
        Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs")
    End Function
End Class
