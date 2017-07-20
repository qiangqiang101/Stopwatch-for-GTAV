Imports GTA
Imports GTA.Math
Imports INMNativeUI
Imports System.Windows.Forms
Imports SpeedTest.Helper
Imports System.Drawing
Imports MapEditor

Public Class SpeedTest
    Inherits Script

    Public Shared player As Player
    Public Shared playerPed As Ped
    Public Shared resultPath As String = Application.StartupPath & "\scripts\Stopwatch Files\"
    Public Shared mapFile As String = Application.StartupPath & "\scripts\Stopwatch Files\maps.txt"
    Public Shared settingFile As String = Application.StartupPath & "\scripts\Stopwatch Files\settings.cfg"
    Public Shared MPH, KPH, TopSpeed As Single
    Public Shared NaughtTo60, TotalTime As String
    Public Shared StopWatch, NaughtTo60SW As Stopwatch
    Public Shared ModActive As Boolean = False
    Public Shared ModStart As Boolean = False
    Public Shared AcrossTheLined As Boolean = False
    Public Shared StartLine, AcrossTheLine As Vector3
    Public Shared StartLineHeading As Single
    Public Shared CarName, SelectedMap As String
    Public Shared WithEvents stMenu, mapMenu, rsMenu As UIMenu
    Public Shared _menuPool As MenuPool
    Public Shared itemMap As New UIMenuItem("Map")
    Public Shared itemResult As New UIMenuItem("Results")
    Public Shared itemEnable As UIMenuItem
    Public Shared itemSpeedo As UIMenuItem
    Public Shared itemStartStop As UIMenuItem
    Public Shared MapParameters As String() = {"[name]", "[start]", "[finish]", "[heading]"}
    Public Shared ResultParameters As String() = {"[name]", "[hash]", "[topspeed]", "[zero_sixty]", "[total]"}
    Public Shared FinishBlip As Blip
    Public Shared MarkerDistance As Single
    Public Shared Rewind As New List(Of Vector3)()
    Public Shared Rewind2 As New List(Of Single)()
    Public Shared ResultList As SortedList = New SortedList()
    Public Shared Config As ScriptSettings = ScriptSettings.Load("scripts\Stopwatch Files\settings.cfg")

    Public Sub New()
        Try
            My.Settings.Speedometer = Config.GetValue(Of String)("SETTING", "SPEEDOMETER", "MPH")
            My.Settings.Save()
            StopWatch = New Stopwatch
            NaughtTo60SW = New Stopwatch
            TopSpeed = 0
            _menuPool = New MenuPool()
            CreateMainMenu()
            CreateMapMenu()
            CreateResultMenu()
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub UpdateTimerBars()
        On Error Resume Next
        If My.Settings.Speedometer = "MPH" Then
            DrawText("VEHICLE NAME    " & CarName, New Drawing.PointF(50, 160), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("SPEED                 " & MPH.ToString("#") & " MPH", New Drawing.PointF(50, 180), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("TOP SPEED          " & TopSpeed.ToString("#") & " MPH", New Drawing.PointF(50, 200), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("TIME                   " & TotalTime.ToString(), New Drawing.PointF(50, 220), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("0 - 60 MPH        " & NaughtTo60.ToString(), New Drawing.PointF(50, 240), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
        Else
            DrawText("VEHICLE NAME    " & CarName, New Drawing.PointF(50, 160), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("SPEED                 " & KPH.ToString("#") & " KPH", New Drawing.PointF(50, 180), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("TOP SPEED          " & ConvertMPH2KPH(TopSpeed).ToString("#") & " KPH", New Drawing.PointF(50, 200), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("TIME                   " & TotalTime.ToString(), New Drawing.PointF(50, 220), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
            DrawText("0 - 100 KPH        " & NaughtTo60.ToString(), New Drawing.PointF(50, 240), 0.6, Color.White, GTAFont.Title, GTAFontAlign.Left, GTAFontStyleOptions.Outline)
        End If
    End Sub

    Public Shared Sub CreateMainMenu()
        Try
            stMenu = New UIMenu("", "STOPWATCH", New Point(0, -107))
            Dim Rectangle = New UIResRectangle()
            Rectangle.Color = Color.FromArgb(0, 0, 0, 0)
            stMenu.SetBannerType(Rectangle)
            _menuPool.Add(stMenu)
            itemEnable = New UIMenuItem("Enable")
            With itemEnable
                .SetRightLabel("False")
            End With
            stMenu.AddItem(itemEnable)
            stMenu.AddItem(itemMap)
            stMenu.AddItem(itemResult)
            itemSpeedo = New UIMenuItem("Speedometer")
            With itemSpeedo
                .SetRightLabel(My.Settings.Speedometer)
            End With
            stMenu.AddItem(itemSpeedo)
            itemStartStop = New UIMenuItem("Start")
            stMenu.AddItem(itemStartStop)
            stMenu.RefreshIndex()
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub CreateMapMenu()
        Try
            Dim format As New Reader(mapFile, MapParameters)
            mapMenu = New UIMenu("", "MAPS", New Point(0, -107))
            Dim Rectangle = New UIResRectangle()
            Rectangle.Color = Color.FromArgb(0, 0, 0, 0)
            mapMenu.SetBannerType(Rectangle)
            _menuPool.Add(mapMenu)
            For i As Integer = 0 To format.Count - 1
                Dim item As New UIMenuItem(format(i)("name"))
                With item
                    .SubString1 = format(i)("start")
                    .SubString2 = format(i)("finish")
                    .SubString3 = format(i)("heading")
                End With
                mapMenu.AddItem(item)
            Next
            mapMenu.RefreshIndex()
            stMenu.BindMenuToItem(mapMenu, itemMap)
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub CreateResultMenu()
        Try
            rsMenu = New UIMenu("", "RESULTS", New Point(UI.WIDTH / 6, 0))
            rsMenu.SetMenuWidthOffset(1000)
            Dim Rectangle = New UIResRectangle()
            Rectangle.Color = Color.FromArgb(0, 0, 0, 0)
            rsMenu.SetBannerType(Rectangle)
            _menuPool.Add(rsMenu)
            rsMenu.AddItem(New UIMenuItem("No Result yet") With {.Enabled = False})
            rsMenu.RefreshIndex()
            stMenu.BindMenuToItem(rsMenu, itemResult)
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub RefreshResultMenu()
        Try
            If My.Settings.Speedometer = "MPH" Then
                If IO.File.Exists(resultPath & SelectedMap & ".txt") Then
                    rsMenu.MenuItems.Clear()
                    ResultList.Clear()
                    rsMenu.Subtitle.Caption = SelectedMap.ToUpper & " RESULTS"
                    Dim format As New Reader(resultPath & SelectedMap & ".txt", ResultParameters)
                    For l As Integer = 0 To format.Count - 1
                        ResultList.Add(format(l)("total"), format(l)("name") & "|" & format(l)("hash") & "|" & format(l)("topspeed") & "|" & format(l)("zero_sixty") & "|" & format(l)("total"))
                    Next
                    Dim key As ICollection = ResultList.Keys
                    Dim k As String
                    For Each k In key
                        Dim itemtext As String = ResultList(k)
                        Dim split() As String = itemtext.Split("|"c)
                        Dim item As New UIMenuItem(split(0))
                        With item
                            .SetRightLabel("Top Speed: " & split(1) & " Mph | 0 - 60 Mph: " & split(2) & " | Total Time: " & split(3))
                        End With
                        rsMenu.AddItem(item)
                    Next
                    rsMenu.RefreshIndex()
                End If
            ElseIf My.Settings.Speedometer = "KPH" Then
                If IO.File.Exists(resultPath & SelectedMap & ".txt") Then
                    rsMenu.MenuItems.Clear()
                    ResultList.Clear()
                    rsMenu.Subtitle.Caption = SelectedMap.ToUpper & " RESULTS"
                    Dim format As New Reader(resultPath & SelectedMap & ".txt", ResultParameters)
                    For l As Integer = 0 To format.Count - 1
                        ResultList.Add(format(l)("total"), format(l)("name") & "|" & format(l)("hash") & "|" & format(l)("topspeed") & "|" & format(l)("zero_sixty") & "|" & format(l)("total"))
                    Next
                    Dim key As ICollection = ResultList.Keys
                    Dim k As String
                    For Each k In key
                        Dim itemtext As String = ResultList(k)
                        Dim split() As String = itemtext.Split("|"c)
                        Dim item As New UIMenuItem(split(0))
                        With item
                            .SetRightLabel("Top Speed: " & ConvertMPH2KPH(split(1)).ToString("#") & " Kph | 0 - 100 Kph: " & split(2) & " | Total Time: " & split(3))
                        End With
                        rsMenu.AddItem(item)
                    Next
                    rsMenu.RefreshIndex()
                End If
            End If
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub ItemSelectHandler(sender As UIMenu, selectedItem As UIMenuItem, index As Integer) Handles stMenu.OnItemSelect
        Try
            If selectedItem Is itemEnable Then
                If ModActive Then
                    ModActive = False
                    selectedItem.SetRightLabel("False")
                    If Not FinishBlip = Nothing Then FinishBlip.Remove()
                Else
                    ModActive = True
                    selectedItem.SetRightLabel("True")
                End If
            ElseIf selectedItem Is itemSpeedo Then
                If itemSpeedo.RightLabel = "MPH" Then
                    itemSpeedo.SetRightLabel("KPH")
                    My.Settings.Speedometer = "KPH"
                    My.Settings.Save()
                    Config.SetValue(Of String)("SETTING", "SPEEDOMETER", "KPH")
                    Config.Save()
                ElseIf itemSpeedo.RightLabel = "KPH" Then
                    itemSpeedo.SetRightLabel("MPH")
                    My.Settings.Speedometer = "MPH"
                    My.Settings.Save()
                    Config.SetValue(Of String)("SETTING", "SPEEDOMETER", "MPH")
                    Config.Save()
                End If
            ElseIf selectedItem.Text = "Start" Then
                If Not SelectedMap = Nothing AndAlso playerPed.IsInVehicle Then
                    selectedItem.Text = "Stop"
                    ModStart = True
                    CarName = playerPed.CurrentVehicle.FriendlyName
                    StopWatch.Reset()
                    StopWatch.Start()
                    NaughtTo60SW.Reset()
                    NaughtTo60 = ""
                    NaughtTo60SW.Start()
                    TopSpeed = 0
                    If playerPed.IsInVehicle Then
                        playerPed.CurrentVehicle.Position = StartLine
                        playerPed.CurrentVehicle.Heading = StartLineHeading
                    Else
                        playerPed.Position = StartLine
                        playerPed.Heading = StartLineHeading
                    End If
                    FinishBlip.ShowRoute = True
                    DisplayHelpTextThisFrame("Press ~INPUT_CELLPHONE_CANCEL~ to reset your vehicle if you stuck.")
                    sender.GoBack()
                End If
            ElseIf selectedItem.Text = "Stop" Then
                selectedItem.Text = "Start"
                ModStart = False
                AcrossTheLined = False
                FinishBlip.ShowRoute = False
                StopWatch.Stop()
                NaughtTo60SW.Stop()
                Rewind.Clear()
                Rewind2.Clear()
            ElseIf selectedItem Is itemResult Then
                    If Not SelectedMap = Nothing Then
                    RefreshResultMenu()
                End If
            End If
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub MapSelectHandler(sender As UIMenu, selectedItem As UIMenuItem, index As Integer) Handles mapMenu.OnItemSelect
        Try
            If ModActive Then
                SelectedMap = selectedItem.Text
                Dim StartingLines() As String = selectedItem.SubString1.Split("|"c)
                StartLine = New Vector3(StartingLines(0), StartingLines(1), StartingLines(2))
                StartLineHeading = selectedItem.SubString3
                Dim FinishLines() As String = selectedItem.SubString2.Split("|"c)
                AcrossTheLine = New Vector3(FinishLines(0), FinishLines(1), FinishLines(2))
                If playerPed.IsInVehicle Then
                    playerPed.CurrentVehicle.Position = StartLine
                    playerPed.CurrentVehicle.Heading = selectedItem.SubString3
                Else
                    playerPed.Position = StartLine
                    playerPed.Heading = selectedItem.SubString3
                End If
                If Not FinishBlip = Nothing Then FinishBlip.Remove()
                FinishBlip = World.CreateBlip(AcrossTheLine)
                FinishBlip.Sprite = BlipSprite.RaceFinish
                FinishBlip.ShowRoute = True
                SetBlipName("Finish", FinishBlip)
                sender.GoBack()
            End If
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Shared Sub OnTick(o As Object, e As EventArgs) Handles MyBase.Tick
        Try
            player = Game.Player
            playerPed = Game.Player.Character

            If StopWatch.IsRunning Then
                TotalTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", New Object() {StopWatch.Elapsed.Hours, StopWatch.Elapsed.Minutes, StopWatch.Elapsed.Seconds, StopWatch.Elapsed.Milliseconds})
                If StopWatch.Elapsed.Seconds.ToString.Contains(0) Then
                    Rewind.Add(playerPed.CurrentVehicle.Position)
                    Rewind2.Add(playerPed.CurrentVehicle.Heading)
                End If
            End If

            If NaughtTo60SW.IsRunning AndAlso MPH >= 60 Then
                NaughtTo60SW.Stop()
                NaughtTo60 = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", New Object() {StopWatch.Elapsed.Hours, StopWatch.Elapsed.Minutes, StopWatch.Elapsed.Seconds, StopWatch.Elapsed.Milliseconds})
            End If

            If Not SelectedMap = Nothing AndAlso Not ModStart = False Then
                MarkerDistance = World.GetDistance(playerPed.Position, AcrossTheLine)
                World.DrawMarker(MarkerType.VerticalCylinder, AcrossTheLine, Vector3.Zero, Vector3.Zero, New Vector3(10.0, 10.0, 10.0), Color.LightPink)
            End If

            If ModActive AndAlso ModStart AndAlso playerPed.IsInVehicle Then
                MPH = ((playerPed.CurrentVehicle.Speed * 3600) / 1609.344)
                KPH = ConvertMPH2KPH(MPH)
                If TopSpeed < MPH Then
                    TopSpeed = TopSpeed + 1
                End If
                If AcrossTheLined = False Then
                    If MPH >= 5 Then
                        UpdateTimerBars()
                        StopWatch.Start()
                    Else
                        StopWatch.Stop()
                        UpdateTimerBars()
                    End If
                Else
                    StopWatch.Stop()
                    UpdateTimerBars()
                    FinishBlip.ShowRoute = False
                    Rewind.Clear()
                    Rewind2.Clear()
                End If
                If MarkerDistance < 10.0 Then
                    AcrossTheLined = True
                    Wait(2000)
                    Update(SelectedMap & ".txt", CarName, TopSpeed, NaughtTo60, TotalTime)
                    itemStartStop.Text = "Start"
                    ModStart = False
                    AcrossTheLined = False
                    FinishBlip.ShowRoute = False
                    StopWatch.Stop()
                    NaughtTo60SW.Stop()
                    Rewind.Clear()
                    Rewind2.Clear()
                    RefreshResultMenu()
                    rsMenu.Visible = True
                End If
                If Game.IsControlJustReleased(0, GTA.Control.PhoneCancel) Then
                    playerPed.CurrentVehicle.Position = Rewind.Last
                    playerPed.CurrentVehicle.Heading = Rewind2.Last
                    For i As Integer = 0 To Rewind.Count - 1
                        Rewind.RemoveAt(i)
                        Rewind2.RemoveAt(i)
                    Next
                End If
            End If
            _menuPool.ProcessMenus()
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Sub OnKeyDown(o As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        Try
            If e.KeyCode = Config.GetValue(Of Keys)("CONTROL", "SECONDARY", Keys.S) AndAlso e.Modifiers = Config.GetValue(Of Keys)("CONTROL", "PRIMARY", Keys.Shift) Then
                If Not _menuPool.IsAnyMenuOpen Then
                    stMenu.Visible = Not stMenu.Visible
                End If
                Game.FadeScreenIn(300)
            End If
        Catch ex As Exception
            Logger.Log(ex.Message & " " & ex.StackTrace)
        End Try
    End Sub

    Public Sub OnAbort() Handles Me.Aborted
        If Not FinishBlip = Nothing Then FinishBlip.Remove()
        Game.FadeScreenIn(300)
    End Sub
End Class
