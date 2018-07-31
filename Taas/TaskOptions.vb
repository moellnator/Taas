Public Class TaskOptions

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property Empty As TaskOptions
        Get
            Return New TaskOptions
        End Get
    End Property

End Class
