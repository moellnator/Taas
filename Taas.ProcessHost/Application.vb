Module Application

    Public Client As TaskClient

    Sub Main()
        Client = TaskClient.Connect(Environment.GetCommandLineArgs(1))
        Client.Run()
    End Sub

End Module
