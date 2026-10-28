Imports System.Threading
Imports System.Web.Script.Serialization

''' <summary>
''' Simula GET /empresas?canal={canal} con header X-Api-Key.
''' ?delayMs= y ?forceError=1 para demora/error.
''' </summary>
Public Class EmpresasHandler
    Implements IHttpHandler

    Private Shared ReadOnly Serializer As New JavaScriptSerializer()

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "application/json"
        context.Response.Charset = "utf-8"

        Dim canal = Trim(If(context.Request.QueryString("canal"), String.Empty))
        If String.IsNullOrWhiteSpace(canal) Then
            WriteError(context, 400, "Parámetro canal es requerido.")
            Return
        End If

        Dim apiKey = If(context.Request.Headers("X-Api-Key"), String.Empty)
        Dim expectedKey = ConfigurationManager.AppSettings("ApiKey." & canal.ToUpperInvariant())

        If String.IsNullOrEmpty(expectedKey) OrElse Not String.Equals(apiKey, expectedKey, StringComparison.Ordinal) Then
            WriteError(context, 401, "API key inválida o canal no autorizado.")
            Return
        End If

        Dim delayMs As Integer
        If Integer.TryParse(context.Request.QueryString("delayMs"), delayMs) AndAlso delayMs > 0 Then
            Thread.Sleep(Math.Min(delayMs, 30000))
        End If

        If String.Equals(context.Request.QueryString("forceError"), "1", StringComparison.OrdinalIgnoreCase) Then
            WriteError(context, 500, "Error simulado del servicio.")
            Return
        End If

        Dim empresas = New List(Of Object) From {
            New With {.id = canal & "-001", .nombre = "Empresa Agua (" & canal & ")", .logoUrl = "https://via.placeholder.com/80?text=Agua"},
            New With {.id = canal & "-002", .nombre = "Empresa Luz (" & canal & ")", .logoUrl = "https://via.placeholder.com/80?text=Luz"},
            New With {.id = canal & "-003", .nombre = "Empresa Telco (" & canal & ")", .logoUrl = "https://via.placeholder.com/80?text=Telco"}
        }

        context.Response.StatusCode = 200
        context.Response.Write(Serializer.Serialize(empresas))
    End Sub

    Private Shared Sub WriteError(context As HttpContext, statusCode As Integer, message As String)
        context.Response.StatusCode = statusCode
        context.Response.TrySkipIisCustomErrors = True
        context.Response.Write(Serializer.Serialize(New With {.error = message}))
    End Sub
End Class
