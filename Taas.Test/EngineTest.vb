Imports Taas.BackEnd
Imports Taas.Common.Logging
Imports Taas.HostProcess

<TestClass()> Public Class EngineTest

    Public Class MockTask : Inherits TaskPayload

        Public Overrides Sub Execute()
            Logger.Warning("++++++ TASK EXECUTION +++++++")
            Threading.Thread.Sleep(1000)
        End Sub

    End Class

    Public Class FailTask : Inherits TaskPayload

        Public Overrides Sub Execute()
            Logger.Warning("++++++ TASK EXECUTION +++++++")
            Throw New Exception("This is a test.")
        End Sub

    End Class

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Execute()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Logger.AddDevice(CommonTest.TraceLogDevice.GetInstance)
        Logger.Verbosity = Level.Debug
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
        Logger.AwaitQueueDrained()
    End Sub

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Fail()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+FailTask") With {
            .ExecuteWithoutShell = False
        }
        Logger.AddDevice(CommonTest.TraceLogDevice.GetInstance)
        Logger.Verbosity = Level.Debug
        Using t As New TaskServer
            t.Initialize(e, o)
            AwaitTaskState(t, TaskState.Initialized)
            t.Execute()
            Assert.AreEqual(TaskState.Executing, e.Tasks.First.State)
            AwaitTaskState(t, TaskState.Failed, 5000)
            Assert.AreEqual(t.LastException.Message, "[Exception] This is a test.")
        End Using
        Assert.AreEqual(TaskState.Disposed, e.Tasks.First.State)
        Logger.AwaitQueueDrained()
    End Sub

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Abort()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Logger.AddDevice(CommonTest.TraceLogDevice.GetInstance)
        Logger.Verbosity = Level.Debug
        Using t As New TaskServer
            t.Initialize(e, o)
            AwaitTaskState(t, TaskState.Initialized)
            t.Execute()
            t.Abort()
            Assert.AreEqual(TaskState.Aborting, e.Tasks.First.State)
            AwaitTaskState(t, TaskState.Aborted, 5000)
        End Using
        Assert.AreEqual(TaskState.Disposed, e.Tasks.First.State)
        Logger.AwaitQueueDrained()
    End Sub

    <TestMethod(), TestCategory("MockUpTask")> Public Sub Pause()
        Dim e As New TestEngine
        Dim o As New TaskOptions("Taas.Test.dll", "Taas.Test.EngineTest+MockTask") With {
            .ExecuteWithoutShell = False
        }
        Logger.AddDevice(CommonTest.TraceLogDevice.GetInstance)
        Logger.Verbosity = Level.Debug
        Using t As New TaskServer
            t.Initialize(e, o)
            AwaitTaskState(t, TaskState.Initialized)
            t.Execute()
            Assert.AreEqual(TaskState.Executing, e.Tasks.First.State)
            t.Pause()
            Assert.AreEqual(TaskState.Paused, e.Tasks.First.State)
            t.Execute()
            Assert.AreEqual(TaskState.Executing, e.Tasks.First.State)
            AwaitTaskState(t, TaskState.Finished, 5000)
        End Using
        Assert.AreEqual(TaskState.Disposed, e.Tasks.First.State)
        Logger.AwaitQueueDrained()
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

        Public Overrides Sub TaskEventSink(sender As Object, e As TaskEventArgs)
            If Not Me._tasks.Contains(sender) Then
                Me._tasks.Add(sender)
            End If
        End Sub

    End Class

End Class