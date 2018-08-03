Namespace API
    Public Class APIHost

        Public Shared ReadOnly CurrentProvider As IProvider

        Shared Sub New()
            If Environment.OSVersion.VersionString.Contains("Windows") Then
                CurrentProvider = WinAPIProvider.GetInstance
            Else
                Throw New NotSupportedException("Currently only Microsoft Windows NT operating systems are supported.")
            End If
        End Sub

    End Class

End Namespace
