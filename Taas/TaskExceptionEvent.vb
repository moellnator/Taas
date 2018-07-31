Public Class TaskExceptionEventArgs : Inherits TaskEventArgs

    Public ReadOnly Property Exception As Exception

    Public Sub New(exception As Exception)
        MyBase.New(TaskState.Failed)
        Me.Exception = exception
    End Sub

End Class
