Public Class TaskOptions

    Public Property ExecuteWithoutShell As Boolean = True
    Public ReadOnly Property Library As String
    Public ReadOnly Property ClassName As String

    Public Sub New(libray As String, className As String)
        Me.Library = libray
        Me.ClassName = className
    End Sub

End Class
