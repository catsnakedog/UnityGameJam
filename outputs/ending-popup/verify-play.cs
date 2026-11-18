string report = await GameDevEndingPopupChecks.RunPlay();
System.IO.File.WriteAllText("outputs/ending-popup/play-checks.txt", report);
return report;
