param (
    [string]$openResult = "n"
)
try
{
	write-output $openResult
    $XsltSettings = New-Object System.Xml.Xsl.XsltSettings($true, $false); 
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath.Load("allgames.xslt",$XsltSettings, $null)
    $XslPath.Transform("nswdb.xml", "allgames.xml")
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath.Load("missinggames.xslt",$XsltSettings, $null)
    $XslPath.Transform("nswdb.xml", "missinggames.xml")
    $XslPath = New-Object System.Xml.Xsl.XslCompiledTransform
    $XslPath.Load("games.xslt")
    $XslPath.Transform("SBM_Local.xml", "SBM_Local.html")
    $XslPath.Transform("SBM_NSP_Local.xml", "SBM_NSP_Local.html")
    $XslPath.Transform("missinggames.xml", "missing.html")
    $XslPath.Transform("allgames.xml", "allgames.html")
    
    Write-Host "Done !"

    if ($openResult -eq "y") {
        ./index.html
    }
}
catch
{
    Write-Host $_.Exception -ForegroundColor Red
}