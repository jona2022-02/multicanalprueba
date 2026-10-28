Public Class EmpresasApiException
    Inherits Exception

    Public ReadOnly Property StatusCode As Integer?

    Public Sub New(mensaje As String, Optional statusCode As Integer? = Nothing, Optional inner As Exception = Nothing)
        MyBase.New(mensaje, inner)
        Me.StatusCode = statusCode
    End Sub
End Class
