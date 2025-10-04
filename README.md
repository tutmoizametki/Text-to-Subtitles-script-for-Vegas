# Text to Subtitles script for Vegas Pro
A script that exports generated text as subtitles in Vegas   
Script should be putten in "Script Menu" folder in Vegas installation folder. Example of path: "C:\Program Files\VEGAS\VEGAS Pro 22.0\Script Menu"  
After copiing script - restart Vegas  

Script has some setting, that can be changed:  
onlyFirstTrackWithSubtitlesIsChecked = 0;  
0 = all tracks will be scanned => all generated text will be gathered => text will be sorted by start date and processed.  
1 = only first track with generated text will be used. All other ignored.  
Any other number will be treated as 0.

trackName = null;  
if trackName = null; then all tracks will be used.  
if trackName equls to some text, for example: trackName = "Subtitles from Speech"; then only tracks with name "Subtitles from Speech" will be used. Text is case-INsensitive, so "Subtitles" will be equal to "SUBTITLES"  

onlySellectedSubtitles = 0;  
0 = all subtitles that are not filtered by previous settings will be added in file.  
1 = only highlited (selected?) generated text and not filtered by previous settings will be added in file.  
Any other number will be treated as 0.  

whatToDoWithOtherlap = 0;  
0 = nothing will be done with overlaping. All text will be sorted and putten in file.  
1 = if text_1 is overlapping text_2 even for 1 millisecond, then text_2 won't be added in file.  
2 = if text_1 is partially overlapping text_2, then start time of text_2 will be moved at end of text_1. If text_1 is fully overlapping text_2, then text_2 won't be added in file.  
Any other number will be treated as 0.  
