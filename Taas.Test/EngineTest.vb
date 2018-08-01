Imports Taas.BackEnd
Imports Taas.HostProcess

<TestClass()> Public Class EngineTest

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Execute()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Using t As New TaskServer
            t.Initialize(e, o)
            Assert.AreEqual(e.Tasks.Count, 1)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Initialized)
            t.Execute()
            Assert.AreEqual(e.Tasks.First.State, TaskState.Running)
            AwaitTask(t, 5000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Finished)
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    Public Class MockTask : Inherits TaskPayload

        Public Overrides Sub Execute()
            Console.WriteLine("Executed")
        End Sub

    End Class

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Abort()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Using t As New TaskServer
            t.Initialize(e, o)
            t.Execute()
            t.Abort()
            AwaitTask(t, 1000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Aborted)
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    '<TestMethod(), TestCategory("MockUpTask")> Public Sub Pause()
    '    Dim e As New TestEngine
    '    Using t As New TaskServer
    '        t.Initialize(e, TaskOptions.Empty)
    '        t.Execute()
    '        t.Pause()
    '        Assert.AreEqual(e.Tasks.First.State, TaskState.Paused)
    '        t.Execute()
    '        AwaitTask(t, 1000)
    '        Assert.AreEqual(e.Tasks.First.State, TaskState.Finished)
    '    End Using
    '    Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    'End Sub

    Private Sub AwaitTask(task As TaskServer, timeOut As Double)
        Dim stopWatch As New Stopwatch
        stopWatch.Start()
        While task.State = TaskState.Running
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