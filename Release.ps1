param
(
    $majorBuild = $false
)

Function FetchCurrentBranchName {
    $params = ("rev-parse --abbrev-ref HEAD").Split(" ")
    $branchName = & git $params

    if ($branchName -ne 'refs/heads/master' -and $branchName -ne 'master') {
        throw 'Must be master branch'
    }

    return $branchName
}

Function Natural-Sort($Object) {
    # http://stackoverflow.com/questions/5427506/how-to-sort-by-file-name-the-same-way-windows-explorer-does
    return $Object | Sort-Object -Property { [regex]::Replace($_, '\d+', { $args[0].Value.PadLeft(20) }) } -Descending
}

Function Fetch-ReleaseBranches() {
    $branches = @()

    (git show-ref --head) | % {
        if ($_ -match "release\/[0-9]+\.([0-9]+)") {
            $branches += $Matches[0]
        }
    }

    return Natural-Sort $branches
}

Function Get-ReleaseVersion() {
    $branches = @(Fetch-ReleaseBranches)
    $lastBranch = $branches[0]
    Write-Host "Detected last branch: $lastBranch"

    if ($lastBranch -match "[0-9]+\.([0-9]+)") {
        $versionNodes = $Matches[0].Split(".");
        $major= [int]$versionNodes[0]
        $minor = [int]$versionNodes[-1]
        
        if ($majorBuild -eq $true) {
            $major = $major + 1
            $minor = 0
        } else {
            $minor = $minor + 1
        }
        return "$major.$minor"
    }

    # return initial release version as there are no other release branches yet
    return "1.0"
}

Function CreateReleaseBranch($version) {
    Write-Host "###### Create new branch: release/$version"
    $branchParams = @('checkout', '-b', "release/$version")
    & git $branchParams

    Write-Host "###### Push branch to remote with tracking enabled"
    $pushParams = @('push', '-u', 'origin', "release/$version", "--quiet")
    & git $pushParams    
}

Function CreateBranch {
    $branchName = FetchCurrentBranchName
    Write-Host "Current branch: $branchName"
    
    if ($branchName -eq 'refs/heads/master' -or $branchName -eq 'master') {
        $version = Get-ReleaseVersion
        CreateReleaseBranch $version
        Write-Host "###### Created new branch: release/$version"
    } else {
        throw 'Must be in master branch to create release'
    }
}

CreateBranch
