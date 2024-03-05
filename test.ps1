Get-ChildItem -recurse | 
	where { $_.name -ilike "*.Tests*.csproj" } | 
	foreach {
		cd $_.DirectoryName
		dotnet test
		if (!$result.Successful) {
			$errorMessage = ""
			if ($result.Error -ne $null) {
				$errorMessage = $result.Error.Message
			}
			Write-Host "Test run / build error"
			Write-Host $errorMessage
			cd ..
			Exit -1
		}
		cd ..;}
        Exit 0