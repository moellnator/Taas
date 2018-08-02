
Namespace IPC

    Public Delegate Sub PipeDataEvent(sender As Object, e As PipeDataEventArgs)

    Public Class PipeDataEventArgs : Inherits EventArgs

        Public ReadOnly Data As Byte()

        Public Sub New(data As Byte())
            Me.Data = data
        End Sub

    End Class

End Namespace
