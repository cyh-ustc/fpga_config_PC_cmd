Module Module1
    Dim comport As IO.Ports.SerialPort
    Dim WriteBuf(256) As Byte
    Sub Main(ByVal argv As String())
        Initcomport()
        Connect(argv(0))
        CommandHandler()
    End Sub
    Sub InitComport()
        comport = New IO.Ports.SerialPort
        comport.BaudRate = 128000
        comport.ReadTimeout = 50
        comport.WriteTimeout = 50
    End Sub
    Sub Connect(ByVal comportname As String)
        comport.PortName = comportname
        comport.Open()
        Console.WriteLine("connected")
    End Sub
    Sub Config(ByVal bfilename As String)
        Dim filestream As IO.FileStream
        Dim i, blocks As Integer
        Dim timeoutcounter As Integer
        Dim lastbyte As Byte
        'Console.WriteLine("start jtag config")
        SendFrame(28) 'start jtag config
        Threading.Thread.Sleep(50)
        Try
            filestream = IO.File.Open(bfilename, IO.FileMode.Open)
        Catch ArgumentException As Exception
            Console.Error.WriteLine("Open file failed")
            Exit Sub
        End Try
        blocks = filestream.Length / 48
        If filestream.Length Mod 48 <> 0 Then blocks += 1
        timeoutcounter = 0
        For i = 0 To blocks - 1
            WriteBuf(2) = filestream.Read(WriteBuf, 8, 48)
            If i = blocks - 1 Then
                lastbyte = WriteBuf(WriteBuf(2) + 7)
                If WriteBuf(2) <> 0 Then WriteBuf(2) -= 1
            End If
            SendFrame(22)
            While True
                Try
                    If comport.ReadChar() = 30 Then
                        timeoutcounter = 0
                        Exit While
                    End If
                Catch TimeoutException As Exception
                    timeoutcounter += 1
                    If timeoutcounter > 100 Then
                        Console.Error.WriteLine("Config time out")
                        filestream.Close()
                        Exit Sub
                    End If
                    Continue While
                End Try
            End While
        Next
        filestream.Close()
        WriteBuf(2) = lastbyte
        SendFrame(25)
        SendFrame(30)
        Console.WriteLine("finished")
    End Sub
    Sub CommandHandler()
        Dim command As String
        Dim bfilename As String
        Dim addr As String
        While True
            command = Console.ReadLine
            Select Case command
                Case "exit"
                    End
                Case "config"
                    bfilename = Console.ReadLine
                    Console.ReadLine()
                    Config(bfilename)
                Case "read"
                    addr = Console.ReadLine
                    Console.ReadLine()
                    ReadFPGA(addr)
                Case "write"
                    addr = Console.ReadLine
                    WriteFPGA(addr)
            End Select

        End While
    End Sub
    Sub SendFrame(ByVal command As Byte)
        WriteBuf(3) = command
        comport.Write(WriteBuf, 0, 64)
    End Sub
    Sub ReadFPGA(ByVal addr As String)
        Dim adr As UInt16
        Dim val(4) As Byte
        Dim result As Int32
        While True
            Try
                comport.ReadByte()
            Catch TimeoutException As Exception
                Exit While
            End Try
        End While
        adr = Convert.ToUInt16(addr)
        WriteBuf(4) = adr Mod 256
        WriteBuf(5) = adr / 256
        SendFrame(51)
        comport.Read(val, 0, 4)
        result = val(0) + val(1) * 256 + val(2) * 256 * 256 + val(3) * 256 * 256 * 256
        Console.WriteLine(result.ToString)
    End Sub
    Sub WriteFPGA(ByVal addr As String)
        Dim adr As UInt16
        Dim result As Int32
        adr = Convert.ToUInt16(addr)
        WriteBuf(4) = adr Mod 256
        WriteBuf(5) = adr / 256
        result = Convert.ToUInt32(Console.ReadLine())
        WriteBuf(8) = result Mod 256
        result = result / 256
        WriteBuf(9) = result Mod 256
        result = result / 256
        WriteBuf(10) = result Mod 256
        result = result / 256
        WriteBuf(11) = result Mod 256
        SendFrame(55)
        Console.WriteLine("writefinished")
    End Sub
End Module
