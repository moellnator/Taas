Public Class TaskClientException : Inherits Exception

    Private _trace As String

    Public Sub New(message As String, stackTrace As String)
        MyBase.New(message)
        Me._trace = stackTrace
    End Sub

    Public Overrides ReadOnly Property StackTrace As String
        Get
            Return Me._trace
        End Get
    End Property

End Class
