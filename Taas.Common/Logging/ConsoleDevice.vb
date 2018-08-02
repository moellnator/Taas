Namespace Logging
    Public Class ConsoleDevice : Implements IDevice

        Private Shared ReadOnly _COLORS As ConsoleColor() = {
            ConsoleColor.Red,
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.Blue,
            ConsoleColor.White
        }

        Private Shared ReadOnly _SYMBOLS As String() = {
            "CRT", "ERR", "WRN", "INF", "DBG"
        }

        Private Shared _instance As ConsoleDevice

        Public Shared ReadOnly Property Instance As ConsoleDevice
            Get
                If _instance Is Nothing Then _instance = New ConsoleDevice
                Return _instance
            End Get
        End Property

        Private Sub New()
        End Sub

        Public Sub AddEntry(entry As Entry) Implements IDevice.AddEntry
            Console.ForegroundColor = ConsoleColor.Gray
            Console.Write(entry.TimeStamp.ToLongTimeString & "  ")
            Console.ForegroundColor = _COLORS(entry.Level)
            Console.Write(_SYMBOLS(entry.Level) & "  ")
            Console.ForegroundColor = ConsoleColor.Gray
            Console.WriteLine(entry.Message)
        End Sub

    End Class

End Namespace
