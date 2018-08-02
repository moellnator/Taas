Imports Taas.BackEnd
Imports Taas.HostProcess

<TestClass()> Public Class EngineTest

    Public Class MockTask : Inherits TaskPayload

        Public Overrides Sub Execute()
            Common.Logging.Logger.Warning("++++++ TASK EXECUTION +++++++")
            Threading.Thread.Sleep(1000)
        End Sub

    End Class

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Execute()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Using t As New TaskServer
            t.Initialize(e, o)
            Assert.AreEqual(TaskState.Initializing, t.State)
            AwaitTaskState(t, TaskState.Initialized)
            Assert.AreEqual(e.Tasks.Count, 1)
            t.Execute()
            Assert.AreEqual(TaskState.Executing, e.Tasks.First.State)
            AwaitTaskState(t, TaskState.Finished, 5000)
        End Using
        Assert.AreEqual(TaskState.Disposed, e.Tasks.First.State)
    End Sub

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Abort()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Using t As New TaskServer
            t.Initialize(e, o)
            AwaitTaskState(t, TaskState.Initialized)
            t.Execute()
            t.Abort()
            Assert.AreEqual(TaskState.Aborting, e.Tasks.First.State)
            AwaitTaskState(t, TaskState.Aborted, 5000)
        End Using
        Assert.AreEqual(TaskState.Disposed, e.Tasks.First.State)
    End Sub

    Private Sub AwaitTaskState(task As TaskServer, state As TaskState, Optional timeOut As Double = 1000)
        Dim stopWatch As New Stopwatch
        stopWatch.Start()
        While Not task.State = state
            Threading.Thread.Sleep(1)
            If stopWatch.ElapsedMilliseconds >= timeOut Then Assert.Fail("Waiting for task has timed out (" & stopWatch.ElapsedTicks & ").")
        End While
        stopWatch.Stop()
    End Sub

    Public Class TestEngine : Inherits TaskEngine

        Private _tasks As New List(Of TaskServer)
        Public ReadOnly Property Tasks As IReadOnlyList(Of TaskServer)
            Get
                Return Me._tasks
            End Get
        End Property

        Private _last_error As Exception = Nothing
        Public ReadOnly Property LastError As Exception
            Get
                Return _last_error
            End Get
        End Property

        Public Overrides Sub TaskEventSink(sender As Object, e As TaskEventArgs)
            If Not Me._tasks.Contains(sender) Then
                Me._tasks.Add(sender)
            End If

        End Sub

    End Class

End Class