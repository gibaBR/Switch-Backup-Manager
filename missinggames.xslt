<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes"/>
<xsl:param name="xci" select="'SBM_Local.xml'"/>
<xsl:param name="nsp" select="'SBM_NSP_Local.xml'"/>
  
<xsl:template match="/releases">

  <xsl:variable name="docXCI" select="document($xci)"/>
  <xsl:variable name="docNSP" select="document($nsp)"/>

  <xsl:element name="Games">

    <xsl:for-each select="release[region/text()!='JPN']">
      <xsl:sort select="name"/>
      <xsl:variable name="releaseId" select="titleid"/>
      <xsl:variable name="gameName" select="name"/>
        <xsl:choose>
          <!-- <xsl:when test="$docXCI/Games/Game[@TitleID=$releaseId]"> -->
          <xsl:when test="not($docXCI/Games/Game[GameName=$gameName] or $docNSP/Games/Game[GameName=$gameName]) and not($docXCI/Games/Game[@TitleID=$releaseId] or $docNSP/Games/Game[@TitleID=$releaseId])">
            <xsl:element name="Game">
              <xsl:attribute name="TitleID"><xsl:value-of select="$releaseId"/></xsl:attribute>
              <xsl:element name="GameName"><xsl:value-of select="name"/></xsl:element>
              <xsl:element name="Developer"><xsl:value-of select="publisher"/></xsl:element>
            </xsl:element>
          </xsl:when>
        </xsl:choose>
      <xsl:text>&#xd;</xsl:text>
    </xsl:for-each>
  </xsl:element>

</xsl:template>
</xsl:stylesheet>
