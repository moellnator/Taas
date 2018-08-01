Public Class TaskPipeProtocol

    Public Shared ReadOnly Property HandShake As Byte = Asc("H"c)
    Public Shared ReadOnly Property Execute As Byte = Asc("R")
    Public Shared ReadOnly Property Library As Byte = Asc("L")
    Public Shared ReadOnly Property ClassInfo As Byte = Asc("C")

    Public Shared Function GetStringArgument(data As Byte()) As String
        Return Text.Encoding.UTF8.GetString(data.Skip(1).ToArray)
    End Function

    Public Shared Function BuildStringCommand(command As Byte, data As String) As Byte()
        Return {command}.Concat(Text.Encoding.UTF8.GetBytes(data)).ToArray
    End Function

End Class
