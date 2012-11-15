﻿Imports System.IO

Public Class FormFCCoordinator

#Region "Markings criteria"
    Private Sub rbnCOptAllChanged() Handles rbnCOptAll.CheckedChanged
        tbxNum.Visible = Not rbnCOptAll.Checked
    End Sub
    Private Sub rbnCOptMoreThanChanged() Handles rbnCOptMoreThan.CheckedChanged
        tbxNum.Location = New Point(tbxNum.Location.X, rbnCOptMoreThan.Location.Y)
    End Sub
    Private Sub rbnCOptSameAsChanged() Handles rbnCOptSameAs.CheckedChanged
        tbxNum.Location = New Point(tbxNum.Location.X, rbnCOptSameAs.Location.Y)
    End Sub
    Private Sub rbnCOptLessThanChanged() Handles rbnCOptLessThan.CheckedChanged
        tbxNum.Location = New Point(tbxNum.Location.X, rbnCOptLessThan.Location.Y)
    End Sub
#End Region

    Public FileArr As String()

    Private Sub tbxNum_TextChanged() Handles tbxNum.TextChanged
        If Integer.TryParse(tbxNum.Text, New Integer) Then
            tbxNum.BackColor = Color.White
        Else
            tbxNum.BackColor = Color.Red
        End If
    End Sub
    Private Sub txtMarkTgt_TextChanged() Handles txtMarkTgt.TextChanged
        If Integer.TryParse(txtMarkTgt.Text, New Integer) Then
            txtMarkTgt.BackColor = Color.White
        Else
            txtMarkTgt.BackColor = Color.Red
        End If
    End Sub

    Private Sub btnOpenManually_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOpenManually.Click
        OFDlg.ShowDialog()
    End Sub

    Private Sub OFDHandler(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OFDlg.FileOk
        If Not String.IsNullOrWhiteSpace(OFDlg.FileName) Then
            FileArr = {OFDlg.FileName}

            ' Clear validation checkboxes (so two sets of files don't get mixed together)
            '   NOTE: Stored file paths are cleared by cbx_Vrify*s event handlers (which are triggered by the property operations below)
            FormQuestionManager.cbx_VrifyQs.Checked = False
            FormQuestionManager.cbx_VrifyAs.Checked = False
            FormQuestionManager.cbx_VrifyMs.Checked = False

            If TabControl1.SelectedIndex = 1 Then
                txtFilePath.Text = FileArr.GetValue(0).ToString
            End If

        End If
    End Sub

    Private Sub SDragDrop_MReset(ByVal Files As String())

        ' Get main file path
        Dim FPath As String = Files.GetValue(0).ToString
        If Files.Count > 1 Then
            MsgBox("Multiple files were drag-dropped on the flashcard coordinator. Only the first file will be used.", MsgBoxStyle.Exclamation)
            Exit Sub
        End If

        ' Input verification
        If txtMarkTgt.BackColor = Color.Red Or tbxNum.BackColor = Color.Red Then
            MsgBox("One of the numerical input values is invalid. Check the red textboxes.")
            Exit Sub
        End If

        ' Check to make sure selected file is a markings file
        Dim FT = Editing.GetFileType(FPath)
        If FT <> "m" Then
            MsgBox("That is not a markings file.")
            Exit Sub
        End If

        ' Get file
        Dim LineList As New List(Of String)
        LineList.AddRange(IO.File.ReadAllLines(FPath))

        ' Check for multiple subjects
        '   If there are multiple subjects, ask the user to pick one
        SubjectChoiceDlg.lbox.Items.Clear()
        For Each L As String In LineList
            If L.StartsWith("[") Then
                If L.EndsWith("]") Then
                    Dim S = L.Substring(1).Substring(0, L.Length - 2)
                    SubjectChoiceDlg.lbox.Items.Add(S)
                    SubjectChoiceDlg.SubjectList.Add(S)
                End If
            End If
        Next
        If SubjectChoiceDlg.lbox.Items.Count > 1 Then
            Dim DR = SubjectChoiceDlg.ShowDialog()
            If DR = DialogResult.Cancel Then
                Exit Sub
            End If
        ElseIf SubjectChoiceDlg.lbox.Items.Count = 1 Then
            SubjectChoiceDlg.Subject = SubjectChoiceDlg.lbox.Items.Item(0).ToString
        End If

        ' Parse through lines and change as needed
        Dim EditMode As Boolean = SubjectChoiceDlg.lbox.Items.Count < 2
        For i = 0 To LineList.Count - 1

            Dim S As String = LineList.Item(i)

            ' Line skips
            If String.IsNullOrWhiteSpace(S) Then
                Continue For
            End If

            ' Edit mode controls
            If S.StartsWith("[") Then
                If EditMode Then
                    EditMode = False
                ElseIf S = "[" & SubjectChoiceDlg.Subject & "]" Then
                    EditMode = True
                End If
            End If

            ' Exception handler
            If Not S.Contains("=") Or Not EditMode Then
                Continue For
            End If

            ' Get strings on both sides of = sign (L includes the =)
            Dim L As String = S.Substring(0, S.IndexOf("=") + 1)
            Dim R As String = S.Substring(L.Length)

            ' Get marking count
            Dim ReqdInt As Integer = CInt(tbxNum.Text)
            Dim RInt As Integer = 0
            If Not Integer.TryParse(R, RInt) Then
                Continue For
            End If

            ' Determine if an edit is needed
            Dim NeedsEdit As Boolean = rbnCOptAll.Checked
            If rbnCOptLessThan.Checked And RInt < ReqdInt Then
                NeedsEdit = True
            ElseIf rbnCOptMoreThan.Checked And RInt > ReqdInt Then
                NeedsEdit = True
            ElseIf rbnCOptSameAs.Checked And RInt <> ReqdInt Then
                NeedsEdit = True
            End If

            ' If no edit needed, continue line loop
            If Not NeedsEdit Then
                Continue For
            End If

            ' If an edit has been performed, re-assemble the line
            LineList.Item(i) = L & txtMarkTgt.Text

        Next

        ' Write to file
        IO.File.WriteAllLines(txtFilePath.Text, LineList)

    End Sub

    Private Sub btnViewMarkings_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewMarkings.Click
        If Not String.IsNullOrWhiteSpace(txtFilePath.Text) Then
            FormQuestionManager.LoadFile(txtFilePath.Text, True)
            FormQuestionManager.Show()
        End If
    End Sub

    Private Sub btnRemark_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRemark.Click
        If Not String.IsNullOrWhiteSpace(txtFilePath.Text) Then
            If FileArr.Count = 1 Then
                SDragDrop_MReset(FileArr)
            End If
        End If
    End Sub

    Private Sub tbInChgd() Handles tbIn.TextChanged

        ' NOTE: This does NOT check the file's format - it merely checks that it exists
        If tbIn.Text.Length = 0 OrElse IO.File.Exists(tbIn.Text) Then ' Hooray for short-circuiting! (Dir(blah) isn't evaluated if tbIn.Text is null)
            tbIn.BackColor = Color.White
        Else
            tbIn.BackColor = Color.Red
        End If

    End Sub

    Private Sub btnGo() Handles Button1.Click

        ' Check that pre-checked file status is OK
        '   NOTE: This checks both the previously checked status (textbox color) and the current status of the files
        '   This is necessary in case the files are moved/deleted between entering their location in (first validation) and activating this method
        If tbIn.BackColor <> Color.White OrElse Not IO.File.Exists(tbIn.Text) Then
            MsgBox("The input file is invalid.")
            Exit Sub
        ElseIf tbOut.BackColor <> Color.White OrElse Not IO.Directory.Exists(tbOut.Text) Then
            MsgBox("The output folder is invalid.")
            Exit Sub
        End If

        ' Generate output file paths
        Dim InputFileName As String = tbIn.Text.Substring(tbIn.Text.LastIndexOf("\"))
        If InputFileName.Contains(".") Then
            InputFileName = InputFileName.Substring(0, InputFileName.IndexOf(".")) & "_"
        Else
            InputFileName &= "_"
        End If
        If Not cbxAppendSubject.Checked Then
            InputFileName = "\"
        End If
        Dim QOutPath As String = tbOut.Text & InputFileName & "questions.txt"
        Dim AOutPath As String = tbOut.Text & InputFileName & "answers.txt"

        ' Create output folder if necessary (note: this doesn't prompt the user as to whether to create the directory)
        '   The try/catch is to notify the user of any failures
        If Not IO.Directory.Exists(tbOut.Text) Then
            Try
                IO.Directory.CreateDirectory(tbOut.Text)
            Catch
                MsgBox("The directory specified for the output files doesn't exist and couldn't be created. No files have been modified.")
                Exit Sub
            End Try
        End If

        ' Alert the user if existing files will be modified at the output location
        If Dir(QOutPath) <> "" AndAlso Dir(AOutPath) <> "" Then
            If MsgBox("There are existing output files in that location. The output generated here will be appended to them. Continue?", MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                MsgBox("No files have been altered.")
                Exit Sub
            End If
        End If

        ' Read file
        Dim LtrStr As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim QuestionCnt As Integer = 0
        Dim AnswerCnt As Integer = 0 ' Used for questions that have multiple valid answers
        Dim CurLineIsQuestion As Boolean = True
        Dim LineQueue_Questions, LineQueue_Answers As New List(Of String) ' A queue, not a stack, according to Dylan @ ACM
        Dim SR As New StreamReader(tbIn.Text)
        While Not SR.EndOfStream

            ' Get line
            Dim Line As String = SR.ReadLine

            ' Skip null lines
            If Line.Length = 0 OrElse String.IsNullOrWhiteSpace(Line) Then
                CurLineIsQuestion = True ' The next valid line is a question
                Continue While
            End If

            ' Add line to line queue
            If CurLineIsQuestion Then

                ' Variable updates
                CurLineIsQuestion = False
                QuestionCnt += 1
                AnswerCnt = 0

                ' Add question to line queue
                LineQueue_Questions.Add(QuestionCnt.ToString & "=" & Line)

            Else

                ' Exception handler
                If AnswerCnt > 25 Then
                    MsgBox("Question " & QuestionCnt & " has more than the maximum number (26) of answers.")
                End If

                ' Add answer to line queue
                LineQueue_Answers.Add(QuestionCnt.ToString & LtrStr.Chars(AnswerCnt) & "=" & Line)

                ' Variable updates (these happen AFTER the current line is added to the queue)
                AnswerCnt += 1

            End If

        End While

        ' Write to output files
        IO.File.AppendAllLines(QOutPath, LineQueue_Questions)
        IO.File.AppendAllLines(AOutPath, LineQueue_Answers)

    End Sub

    Private Sub tbOut_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbOut.TextChanged

        ' Make sure the output folder directory isn't ending with a \, isn't to a file (i.e. it SHOULD NOT contain a "."), is non-null
        Dim outTxt As String = tbOut.Text
        If outTxt.Length = 0 OrElse outTxt.Last = "\" OrElse outTxt.Contains(".") Then
            tbOut.BackColor = Color.Red
        Else
            tbOut.BackColor = Color.White
        End If

    End Sub

End Class