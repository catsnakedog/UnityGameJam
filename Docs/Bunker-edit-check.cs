var result = BunkerChecks.RunEdit();
System.IO.File.WriteAllText("Docs/Bunker-edit.txt", result);
return result;
