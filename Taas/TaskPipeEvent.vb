
Public Delegate Sub TaskPipeDataEvent(sender As Object, e As TaskPipeDataEventArgs)

Public Class TaskPipeDataEventArgs : Inherits EventArgs

    Public ReadOnly Data As Byte()

    Public Sub New(data As Byte())
        Me.Data = data
    End Sub

End Class
