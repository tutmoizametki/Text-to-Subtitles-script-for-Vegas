using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using ScriptPortal.Vegas;

public class EntryPoint
{
    /* All Settings are HERE */
    public int onlyFirstTrackWithSubtitlesIsChecked = 0;            // 0 = all tracks will be scanned => all generated text will be gathered => text will be sorted by start date and processed.
                                                                    // 1 = only first track with generated text will be used. All other ignored.
                                                                    // any other number will be treated as 0.

	public String trackName = null;                                 // if trackName = null; then all tracks will be used.
                                                                    // if trackName equls to some text, for example: trackName = "Subtitles from Speech"; then only tracks with name "Subtitles from Speech" will be used. Text is case-INsensitive, so "Subtitles" will be equal to "SUBTITLES"

    public int onlySellectedSubtitles = 0;                          // 0 = all subtitles that are not filtered by previous settings will be added in file.
                                                                    // 1 = only highlited (selected?) generated text and not filtered by previous settings will be added in file.
                                                                    // any other number will be treated as 0.

    public int whatToDoWithOtherlap = 0;                            // 0 = nothing will be done with overlaping. All text will be sorted and putten in file.
                                                                    // 1 = if text_1 is overlapping text_2 even for 1 millisecond, then text_2 won't be added in file.
                                                                    // 2 = if text_1 is partially overlapping text_2, then start time of text_2 will be moved at end of text_1. If text_1 is fully overlapping text_2, then text_2 won't be added in file.
                                                                    // any other number will be treated as 0.


    /* No Setting lower */
    public void FromVegas(Vegas vegas)
    {
        Project proj = vegas.Project;
        String sProjName;
        String sProjFile = vegas.Project.FilePath;
        if (String.IsNullOrEmpty(sProjFile))
        {
            sProjName = "Untitled";
        }
        else
        {
            sProjName = Path.GetFileNameWithoutExtension(sProjFile);
        }

        String sExportFile = ShowSaveFileDialog("SubRip (*.srt)|*.srt", "Save Closed Caption as Subtitles", sProjName);

        if (null != sExportFile)
        {
            StreamWriter streamWriter = null;
            String sExt = Path.GetExtension(sExportFile);
            if (((null != sExt) && (sExt.ToUpper() != ".SRT")) || null == sExt)
            {
                sExportFile = Path.Combine(Path.GetDirectoryName(sExportFile), Path.GetFileNameWithoutExtension(sExportFile) + ".srt");
            }
            try
            {
                FileStream filestream = null;
                filestream = new FileStream(sExportFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                streamWriter = new StreamWriter(filestream, System.Text.Encoding.UTF8);
                StringBuilder sOut = new StringBuilder();
                List<VideoEvent> eventList = new List<VideoEvent>();
                List<toBeFiltered> textData = new List<toBeFiltered>();
                foreach (Track myTrack in vegas.Project.Tracks)
                {
                    if (myTrack.IsVideo())
                    {

                        if (trackName != null)
                        {
                            String currentTrackName = myTrack.Name;
                            if (currentTrackName != null)
                            {
                                currentTrackName = currentTrackName.Trim().ToLower();
                                trackName = trackName.Trim().ToLower();
                                if (currentTrackName == trackName)
                                {
                                    foreach (TrackEvent evnt in myTrack.Events)
                                    {
                                        if (evnt.ActiveTake.Media.Generator != null)
                                        {
                                            textData = addDataToList(evnt, textData);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (TrackEvent evnt in myTrack.Events)
                            {
                                if (evnt.ActiveTake.Media.Generator != null)
                                {
                                    textData = addDataToList(evnt, textData);
                                }
                            }
                        }

                    }
                    if (onlyFirstTrackWithSubtitlesIsChecked == 1 && textData.Count > 0) break;
                }
                textData = SortList(textData);
                sOut = BuildStringWithData(textData);
                streamWriter.WriteLine(sOut.ToString());
            }
            finally
            {
                if (null != streamWriter)
                {
                    streamWriter.Close();
                }
            }
        }
    }

    List<toBeFiltered> addDataToList(TrackEvent evnt, List<toBeFiltered> textData)
    {
        if (onlySellectedSubtitles == 1)
        {
            if (evnt.Selected)
            {
                OFXStringParameter textParam = evnt.ActiveTake.Media.Generator.OFXEffect.FindParameterByName("Text") as OFXStringParameter;
                toBeFiltered currentTextData = new toBeFiltered();
                currentTextData.text = ConvertRtfToPlainText(textParam.Value);
                currentTextData.stratTime = evnt.Start.Nanos;
                currentTextData.endTime = evnt.Start.Nanos + evnt.Length.Nanos;
                textData.Add(currentTextData);
            }
        }
        else
        {
            OFXStringParameter textParam = evnt.ActiveTake.Media.Generator.OFXEffect.FindParameterByName("Text") as OFXStringParameter;
            toBeFiltered currentTextData = new toBeFiltered();
            currentTextData.text = ConvertRtfToPlainText(textParam.Value);
            currentTextData.stratTime = evnt.Start.Nanos;
            currentTextData.endTime = evnt.Start.Nanos + evnt.Length.Nanos;
            textData.Add(currentTextData);
        }
        return textData;
    }

    String ConvertRtfToPlainText(String rtfContent)
    {
        RichTextBox rtBox = new RichTextBox();
        rtBox.Rtf = rtfContent;
        String text = rtBox.Text;
        text = text.Replace("\r\n", "\n");
        text = text.Replace("\n", " ");
        text = text.Replace("  ", " ");
        return text;
    }

    String TimecodeToSRTString(long timecode)
    {
        Int64 time = Convert.ToInt64(timecode);
        time = time / 10000;
        Int64 hours = time / 36000000;
        Int64 mins = (time - hours * 3600000) / 60000;
        Int64 secs = (time - hours * 3600000 - mins * 60000) / 1000;
        Int64 ssecs = time - hours * 3600000 - mins * 60000 - secs * 1000;
        return String.Format("{0:00}:{1:00}:{2:00},{3:000}", hours, mins, secs, ssecs);
    }


    String ShowSaveFileDialog(String sFilter, String sTitle, String sDefaultFileName)
    {
        SaveFileDialog fileDialog = new SaveFileDialog();

        if (null == sFilter)
        {
            sFilter = "All Files (*.*)|*.*";
        }

        fileDialog.Filter = sFilter;

        if (null != sTitle)
        {
            fileDialog.Title = sTitle;
        }
        fileDialog.CheckPathExists = true;
        fileDialog.AddExtension = true;

        if (null != sDefaultFileName)
        {
            String sDir = Path.GetDirectoryName(sDefaultFileName);
            if (Directory.Exists(sDir))
            {
                fileDialog.InitialDirectory = sDir;
            }
            fileDialog.DefaultExt = Path.GetExtension(sDefaultFileName);
            fileDialog.FileName = Path.GetFileName(sDefaultFileName);
        }

        if (System.Windows.Forms.DialogResult.OK == fileDialog.ShowDialog())
        {
            return Path.GetFullPath(fileDialog.FileName);
        }
        else
        {
            return null;
        }
    }
    List<toBeFiltered> SortList(List<toBeFiltered> textData)
    {
        bool noChangesDone = false;

        while (!noChangesDone)
        {
            noChangesDone = true;
            for (int i = 0; i < textData.Count - 1; i++)
            {
                if (textData[i].stratTime > textData[i + 1].stratTime)
                {
                    toBeFiltered temData = textData[i];
                    textData[i] = textData[i + 1];
                    textData[i + 1] = temData;
                    noChangesDone = false;
                }
            }
        }

        noChangesDone = false;
        if (whatToDoWithOtherlap == 1)
        {
            while (!noChangesDone)
            {
                noChangesDone = true;
                for (int i = 0; i < textData.Count - 1; i++)
                {
                    if (textData[i].endTime > textData[i + 1].stratTime)
                    {
                        textData.RemoveAt(i + 1);
                        noChangesDone = false;
                    }

                }
            }
        }
        else if (whatToDoWithOtherlap == 2)
        {
            while (!noChangesDone)
            {
                noChangesDone = true;
                for (int i = 0; i < textData.Count - 1; i++)
                {
                    if (textData[i].endTime > textData[i + 1].stratTime)
                    {
                        if (textData[i].endTime >= textData[i + 1].endTime)
                        {
                            textData.RemoveAt(i + 1);
                        }
                        else
                        {
                            textData[i + 1].stratTime = textData[i].endTime;
                        }
                        noChangesDone = false;
                    }

                }
            }
        }

        return textData;
    }

    StringBuilder BuildStringWithData(List<toBeFiltered> textData)
    {
        StringBuilder sOut = new StringBuilder();

        int number = 1;
        foreach (toBeFiltered currentData in textData)
        {
            sOut.Append(number + "\r\n");
            sOut.Append(TimecodeToSRTString(currentData.stratTime) + " --> " + TimecodeToSRTString(currentData.endTime) + "\r\n");
            sOut.Append(currentData.text + "\r\n");
            sOut.Append("\r\n");
            number++;
        }
        return sOut;
    }
}

public class toBeFiltered
{
    public string text { get; set; }
    public long stratTime { get; set; }
    public long endTime { get; set; }
}
