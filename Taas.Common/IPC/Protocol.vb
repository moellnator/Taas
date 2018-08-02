Namespace IPC
    Public Class Protocol

        Public Shared ReadOnly Property HandShake As Byte = Asc("H"c)
        Public Shared ReadOnly Property Execute As Byte = Asc("R"c)
        Public Shared ReadOnly Property Library As Byte = Asc("L"c)
        Public Shared ReadOnly Property ClassInfo As Byte = Asc("C"c)
        Public Shared ReadOnly Property Critial As Byte = Asc("E"c)

        Public Shared Function GetStringArgument(data As Byte()) As String
            Return Text.Encoding.UTF8.GetString(data.Skip(1).ToArray)
        End Function

        Public Shared Function BuildStringCommand(command As Byte, data As String) As Byte()
            Return {command}.Concat(Text.Encoding.UTF8.GetBytes(data)).ToArray
        End Function

    End Class

End Namespace
