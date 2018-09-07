<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:template match="/Games">
<html>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<style>
* {
  box-sizing: border-box;
}

#myInput,#myInputDetail {
  background-image: url('searchicon.png');
  background-position: 10px 10px;
  background-repeat: no-repeat;
  width: 100%;
  font-size: 16px;
  padding: 12px 20px 12px 40px;
  border: 1px solid #ddd;
  margin-bottom: 12px;
}

#myTable {
  border-collapse: collapse;
  width: 100%;
  border: 1px solid #ddd;
  font-size: 18px;
}

#myTable th, #myTable td {
  text-align: left;
  padding: 12px;
}

#myTable tr {
  border-bottom: 1px solid #ddd;
}

#myTable tr.header, #myTable tr:hover {
  background-color: #f1f1f1;
}

</style>
</head>
<body>
  <p><a href="index.html">Back</a></p>
  <p><xsl:value-of select="count(Game)"/> games listed.</p>
  <input type="text" id="myInput" onkeyup="myFunction()" placeholder="Search in titles and developer names..." title="Enter a search term" />
  <xsl:choose>
    <xsl:when test="/Games/Game/Description">
      <input type="text" id="myInputDetail" onkeyup="myFunction2()" placeholder="Search in descriptions..." title="Enter a search term" />
    </xsl:when>
  </xsl:choose>
  <script>
  <![CDATA[
function myFunction() {
  var input, filter, table, tr, td, i;
  input = document.getElementById("myInput");
  filter = input.value.toUpperCase();
  table = document.getElementById("myTable");
  tr = table.getElementsByTagName("tr");
  for (i = 0; i < tr.length; i++) {
    tds = tr[i].getElementsByTagName("td");
    display = "none"
    for (j = 0; j < tds.length; j++) {
      td = tds[j]
      if (td && (td.getAttribute("id") == "GameName" || td.getAttribute("id") == "Developer")) {
        if (td.innerHTML.toUpperCase().indexOf(filter) > -1) {
          display = "";
          break;
        }
      }       
    }
    tr[i].style.display = display;
  }
}

function myFunction2() {
  var input, filter, table, tr, td, i;
  input = document.getElementById("myInputDetail");
  filter = input.value.toUpperCase();
  table = document.getElementById("myTable");
  tr = table.getElementsByTagName("tr");
  for (i = 0; i < tr.length; i++) {
    title = tr[i].getAttribute("title");
    if (!filter || (title && title.toUpperCase().indexOf(filter) > -1)) {
        tr[i].style.display = "";
    } else {
      tr[i].style.display = "none";
    }
  }
}
]]>
</script>
  <table id="myTable" >
    <tr bgcolor="#9acd32">
        <xsl:choose>
          <xsl:when test="/Games/Game/Region_Icon">
            <xsl:element name="th">Cover</xsl:element>
          </xsl:when>
        </xsl:choose>
      <th>Title</th>
      <th>Developer</th>
    </tr>
    <xsl:for-each select="Game">
      <xsl:sort select="GameName"/>
      <xsl:element name="tr">
        <xsl:choose>
          <xsl:when test="Description != ''">
            <xsl:attribute name="title"><xsl:value-of select="Description"/></xsl:attribute>
          </xsl:when>
        </xsl:choose>
        <xsl:variable name="icon"><xsl:value-of select="substring-before(substring-after(Region_Icon,', '),']')"/></xsl:variable>
        <xsl:choose>
          <xsl:when test="/Games/Game/Region_Icon">
            <td>
              <xsl:choose>
                <xsl:when test="Region_Icon!=''">
                  <img height="64" width="64" alt="icon" src="{$icon}" />
                </xsl:when>
                <xsl:otherwise>
                  <img height="64" width="64" alt="icon" src="data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw=="/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </xsl:when>
        </xsl:choose>
        <td id="GameName"><xsl:value-of select="GameName"/></td>
        <td id="Developer"><xsl:value-of select="Developer"/></td>
      </xsl:element>
    </xsl:for-each>
  </table>
</body>
</html>
</xsl:template>
</xsl:stylesheet>

