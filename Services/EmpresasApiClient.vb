Imports System.Net
Imports System.Net.Http
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web.Script.Serialization

Public Class EmpresasApiClient
    Private Shared ReadOnly SharedClient As HttpClient = CreateSharedClient()
    Private Shared ReadOnly Serializer As New JavaScriptSerializer()
    Private ReadOnly _http As HttpClient

    Public Sub New()
        _http = SharedClient
    End Sub

    Public Sub New(handler As HttpMessageHandler)
        If handler Is Nothing Then Throw New ArgumentNullException("handler")
        _http = New HttpClient(handler)
    End Sub

    Private Shared Function CreateSharedClient() As HttpClient
        ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol Or SecurityProtocolType.Tls12
        Dim client As New HttpClient()
        client.DefaultRequestHeaders.ExpectContinue = False
        Return client
    End Function

    Public Function ObtenerEmpresas(urlBase As String, apiKey As String, canal As String, timeoutSegundos As Integer) As IList(Of EmpresaPago)
        Return ObtenerEmpresasAsync(urlBase, apiKey, canal, timeoutSegundos).ConfigureAwait(False).GetAwaiter().GetResult()
    End Function

    Public Async Function ObtenerEmpresasAsync(urlBase As String, apiKey As String, canal As String, timeoutSegundos As Integer) As Task(Of IList(Of EmpresaPago))
        If String.IsNullOrWhiteSpace(urlBase) Then Throw New ArgumentException("UrlServicio vacía.", "urlBase")
        If String.IsNullOrWhiteSpace(apiKey) Then Throw New ArgumentException("ApiKey vacía.", "apiKey")
        If String.IsNullOrWhiteSpace(canal) Then Throw New ArgumentException("Canal vacío.", "canal")

        Dim segundos As Integer = If(timeoutSegundos > 0, timeoutSegundos, 5)
        Dim absoluteBase = ResolverUrlAbsoluta(urlBase)
        Dim separator As String = If(absoluteBase.Contains("?"), "&", "?")
        Dim requestUrl As String = absoluteBase.TrimEnd("/"c) & separator & "canal=" & Uri.EscapeDataString(canal.Trim())
        Dim requestUri As New Uri(requestUrl)

        Using request As New HttpRequestMessage(HttpMethod.Get, requestUri)
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey)
            Using cts As New CancellationTokenSource(segundos * 1000)
                Try
                    Using response = Await _http.SendAsync(request, cts.Token).ConfigureAwait(False)
                        Dim body = Await response.Content.ReadAsStringAsync().ConfigureAwait(False)
                        If response.StatusCode = HttpStatusCode.Unauthorized Then
                            Throw New EmpresasApiException("El servicio rechazó la API key del canal.", 401)
                        End If
                        If Not response.IsSuccessStatusCode Then
                            Throw New EmpresasApiException("El servicio respondió " & CInt(response.StatusCode).ToString() & ".", CInt(response.StatusCode))
                        End If
                        Return DeserializeEmpresas(body)
                    End Using
                Catch ex As OperationCanceledException
                    Throw New EmpresasApiException("El servicio no respondió en " & segundos.ToString() & "s (timeout).", Nothing, ex)
                Catch ex As EmpresasApiException
                    Throw
                Catch ex As Exception
                    Throw New EmpresasApiException("No se pudo contactar el servicio de empresas.", Nothing, ex)
                End Try
            End Using
        End Using
    End Function

    ''' <summary>Permite UrlServicio relativa (/empresas) resolviendo contra el host del sitio.</summary>
    Private Shared Function ResolverUrlAbsoluta(urlBase As String) As String
        If Uri.IsWellFormedUriString(urlBase, UriKind.Absolute) Then
            Return urlBase
        End If
        Dim ctx = HttpContext.Current
        If ctx Is Nothing Then
            Throw New InvalidOperationException("UrlServicio relativa requiere HttpContext.")
        End If
        Dim path = urlBase.Trim()
        If Not path.StartsWith("/") Then
            path = "/" & path
        End If
        Return ctx.Request.Url.GetLeftPart(UriPartial.Authority) & path
    End Function

    Private Shared Function DeserializeEmpresas(json As String) As IList(Of EmpresaPago)
        Dim raw = Serializer.Deserialize(Of Object())(json)
        Dim lista As New List(Of EmpresaPago)()
        If raw Is Nothing Then Return lista
        For Each item As Object In raw
            Dim dict = TryCast(item, Dictionary(Of String, Object))
            If dict Is Nothing Then Continue For
            Dim idVal As Object = Nothing
            Dim nomVal As Object = Nothing
            Dim logoVal As Object = Nothing
            dict.TryGetValue("id", idVal)
            dict.TryGetValue("nombre", nomVal)
            dict.TryGetValue("logoUrl", logoVal)
            lista.Add(New EmpresaPago With {
                .Id = Convert.ToString(idVal),
                .Nombre = Convert.ToString(nomVal),
                .LogoUrl = Convert.ToString(logoVal)
            })
        Next
        Return lista
    End Function
End Class
