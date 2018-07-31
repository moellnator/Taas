
Public Delegate Sub TaskEventHandler(sender As Object, e As TaskEventArgs)

Public Class TaskEventArgs : Inherits EventArgs

    Public ReadOnly Property NewState As TaskState

    Public Sub New(newState As TaskState)
        Me.NewState = newState
    End Sub

End Class
