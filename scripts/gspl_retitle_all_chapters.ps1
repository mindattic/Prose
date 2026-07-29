$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$em = [char]8212

function Set-Title([string]$id, [string]$title) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SET QUOTED_IDENTIFIER ON; UPDATE Nodes SET Title=@T, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
    $cmd.Parameters.AddWithValue("@T", $title) | Out-Null
    $cmd.Parameters.AddWithValue("@Id", [guid]$id) | Out-Null
    $n = $cmd.ExecuteNonQuery()
    if ($n -ne 1) { Write-Host "  !! FAILED ($n rows): $title" } else { Write-Host "  ok: $title" }
}

# ---------------- MATTHEW ----------------
Write-Host "MATTHEW"
Set-Title "019FA049-5D94-766F-A919-4623FD605028" "Chapter 1 $em Fourteen, Fourteen, and the Missing Kings"
Set-Title "019FA049-8E60-70CC-BFAB-692BDB97D336" "Chapter 2 $em Stargazers, a Border Crossing, and a Massacre No One Else Recorded"
Set-Title "019FA054-FBB7-7C0A-AFA4-DF042B65F960" "Chapter 3 $em Locusts, Wild Honey, and the Man in the River"
Set-Title "019FA063-93C6-708B-8B3E-90703FF50C84" "Chapter 4 $em Forty Days in the Wilderness and Four Men Who Left Their Nets"
Set-Title "019FA064-CBA6-7FF6-828B-72094212CB22" "Chapter 5 $em Salt, Lamps, and a Law Made Harder, Not Easier"
Set-Title "019FA065-8069-7E01-98D2-686871E63831" "Chapter 6 $em Street-Corner Prayer, Moths, and the Lilies' Wardrobe"
Set-Title "019FA066-1F9A-7E33-8627-91085971065D" "Chapter 7 $em Specks, Planks, and Two Ways to Build a House"
Set-Title "019FA066-CCC6-746B-A184-E781F3461446" "Chapter 8 $em A Centurion's Chain of Command and a Herd of Pigs"
Set-Title "019FA067-8522-77F9-898C-52F3ACA42AD1" "Chapter 9 $em The Tax Man's Dinner Party and the Flute Players at the Door"
Set-Title "019FA068-3D3A-7CE3-BD9C-F0463448908A" "Chapter 10 $em No Bag, No Sandals, No Second Tunic"
Set-Title "019FA068-DA02-71F2-AB5E-E84E36383284" "Chapter 11 $em A Prisoner's Doubt and Three Towns Now in Ruins"
Set-Title "019FA069-844F-7A56-A07C-D5037832F038" "Chapter 12 $em Hungry on the Sabbath, and the Only Sign They Would Get"
Set-Title "019FA06B-7580-76BD-93D9-2ADDCEE9AF4C" "Chapter 13 $em Mustard Seeds, Bad Fish, and Buried Treasure"
Set-Title "019FA06C-2D81-7D7E-87EE-BC3BA620B663" "Chapter 14 $em A Head on a Platter and Bread on a Hillside"
Set-Title "019FA06C-F68B-7BC8-96BA-0F00A6216BD7" "Chapter 15 $em Washed Hands, a Foreign Woman's Argument, and Bread Again"
Set-Title "019FA06D-89CE-7DFF-871E-E5AACFEA94DA" "Chapter 16 $em The Rock, the Keys, and a Word Matthew Alone Uses"
Set-Title "019FA06E-4B5A-7AAB-80B3-3EF0B408119C" "Chapter 17 $em A Face Like the Sun and a Coin in a Fish's Mouth"
Set-Title "019FA06E-E541-76D7-8867-57D1241C3DDC" "Chapter 18 $em Seventy-Seven Times, and a Debt No One Could Repay"
Set-Title "019FA06F-661F-7830-9AD8-BF91C0C5F560" "Chapter 19 $em Grounds for Divorce and a Camel at the Needle"
Set-Title "019FA070-1F63-7891-8ABA-40A617CF7273" "Chapter 20 $em A Denarius Is a Denarius, and Two Men on the Jericho Road"
Set-Title "019FA070-F866-7A49-8157-5E6B429D1C37" "Chapter 21 $em Two Donkeys and the Tables in the Court of the Gentiles"
Set-Title "019FA071-9611-73CB-A9C2-8DD1ADEAD70C" "Chapter 22 $em Whose Head Is on the Coin, and Whose Wife in the Resurrection"
Set-Title "019FA072-2582-769C-A2B5-85E052E09347" "Chapter 23 $em Whitewashed Tombs and the Straining of Gnats"
Set-Title "019FA073-292F-7D88-973F-2FB76C93F677" "Chapter 24 $em Not One Stone Upon Another"
Set-Title "019FA073-7BBC-79E9-B2A8-F4F23309C2A3" "Chapter 25 $em Lamps Out of Oil, Talents Buried, and Sheep Sorted from Goats"
Set-Title "019FA074-4A7C-7119-AF4B-1F2DB39E5FC8" "Chapter 26 $em Thirty Pieces of Silver and a Trial Held at Night"
Set-Title "019FA076-F16B-74ED-8384-66A7A6DE8052" "Chapter 27 $em The Nail, the Titulus, and the Governor Who Washed His Hands"
Set-Title "019FA078-392B-7408-B4A9-4CA5E15931F7" "Chapter 28 $em The Guards, the Stone, and a Rumour That Outlived Them"

# ---------------- MARK (only the 8 still descriptive) ----------------
Write-Host "MARK"
Set-Title "019FA966-FCDC-70EC-B729-D891E6C094DE" "Chapter 1 $em A Voice in the Wilderness and a Man Who Could Not Keep Quiet"
Set-Title "019FA967-0D77-73B8-A0B4-BA4423DF5219" "Chapter 2 $em A Hole in the Roof and Dinner with the Wrong People"
Set-Title "019FA967-1DC4-7B12-8948-FC0C423511D4" "Chapter 3 $em Twelve Names and a House Divided"
Set-Title "019FA967-4F79-781A-A0F2-C090C5D418C8" "Chapter 6 $em A Birthday Gift, and Bread on the Green Grass"
Set-Title "019FA967-9352-7FD4-A6DE-9380C8B29296" "Chapter 10 $em Divorce, a Camel, and a Blind Man Who Threw Off His Cloak"
Set-Title "019FA967-A43B-7A7E-8DCC-A2D3581571FC" "Chapter 11 $em The Fig Tree That Wasn't in Season"
Set-Title "019FA967-B4EF-7F1B-B44A-506365CDE94A" "Chapter 12 $em A Vineyard, a Coin, and Two Small Copper Coins"
Set-Title "019FA967-D620-7761-9914-709A1C2F8240" "Chapter 14 $em An Alabaster Jar and a Young Man Who Ran"

# ---------------- LUKE ----------------
Write-Host "LUKE"
Set-Title "019FA969-7C4A-7ABD-8064-462CC483E632" "Chapter 1 $em An Orderly Account, and Two Pregnancies Nobody Expected"
Set-Title "019FA969-8DD2-71F0-9128-0ECCD459204C" "Chapter 2 $em A Census, a Manger, and a Boy Who Wandered Off"
Set-Title "019FA969-9E9E-7651-9C3B-213724190883" "Chapter 3 $em In the Fifteenth Year of Tiberius: The Verse That Can Be Dated"
Set-Title "019FA969-AF97-7AFA-991B-52064F38E463" "Chapter 4 $em The Scroll, the Hometown, and the Brow of the Hill"
Set-Title "019FA969-C063-70F5-B56D-CA0D956B34FA" "Chapter 5 $em Nets That Broke and a New Patch on an Old Coat"
Set-Title "019FA969-D0A9-713F-9478-4AA57F3D866C" "Chapter 6 $em The Sermon on the Level Ground"
Set-Title "019FA969-E19C-72C6-BB63-117877953132" "Chapter 7 $em A Soldier Who Understood Orders and a Widow at the Gate"
Set-Title "019FA969-F317-737B-98F0-E1CB606D1FF1" "Chapter 8 $em Seed, Storm, and the Women Who Paid for It"
Set-Title "019FA96A-0586-741F-A105-2CB2DEB94ADE" "Chapter 9 $em Five Loaves, a Mountain, and Nowhere to Lay His Head"
Set-Title "019FA96A-17B1-74B5-9EF1-2DBF1B6097A2" "Chapter 10 $em Seventy-Two on the Road, and a Samaritan on It"
Set-Title "019FA96A-2935-7B65-A953-0250EC861825" "Chapter 11 $em A Shorter Prayer and a Neighbour Banging on the Door"
Set-Title "019FA96A-3A6B-7D8C-9960-89F2E212469B" "Chapter 12 $em Five Sparrows for Two Pennies"
Set-Title "019FA96A-4BD0-7EC1-BE40-6B20CFE5E550" "Chapter 13 $em A Tower That Fell and a Fig Tree on Probation"
Set-Title "019FA96A-5DD7-7E47-8245-15A9F5A3CFC4" "Chapter 14 $em Where to Sit, and Who Actually Came"
Set-Title "019FA96A-701D-705E-86AB-2A1586EC255D" "Chapter 15 $em One Sheep, One Coin, One Son"
Set-Title "019FA96A-82EC-7827-8630-C88454A484EE" "Chapter 16 $em The Manager Who Cooked the Books and Was Praised for It"
Set-Title "019FA96A-9507-7DFD-8E43-53E7682266FD" "Chapter 17 $em Ten Healed, One Came Back"
Set-Title "019FA96A-A5E2-7CEA-A466-C3D869572B74" "Chapter 18 $em A Widow Who Wore the Judge Down"
Set-Title "019FA96A-B6EA-72C1-8FC8-CD6FA1ABA179" "Chapter 19 $em A Tax Man Up a Tree and Stones That Would Shout"
Set-Title "019FA96A-C70E-7958-AB73-945FD5159F40" "Chapter 20 $em Answering a Question with a Question"
Set-Title "019FA96A-D7AB-79F9-B44B-5930429E810C" "Chapter 21 $em All She Had to Live On, and a Temple with a Deadline"
Set-Title "019FA96A-E7D2-741F-BCDF-381A086FF909" "Chapter 22 $em Bread, a Kiss, and a Fire in the Courtyard"
Set-Title "019FA96A-F7DB-7C55-A3FB-B537AC6B6848" "Chapter 23 $em Two Hearings, One Verdict"
Set-Title "019FA96B-0852-788F-A080-F71808F0DC08" "Chapter 24 $em Seven Miles to Emmaus, Wherever That Is"

# ---------------- JOHN ----------------
Write-Host "JOHN"
Set-Title "019FA96C-1C8D-7943-95A0-EC520EBA1EA4" "Chapter 1 $em In the Beginning, Again"
Set-Title "019FA96C-2D2C-71D5-B6CB-4B13CC2CFD5B" "Chapter 2 $em Six Stone Jars and a Temple Cleared Too Early"
Set-Title "019FA96C-3D69-7470-ABE8-8DB126CEB6FB" "Chapter 3 $em A Night Visit and a Word That Means Two Things"
Set-Title "019FA96C-4DDB-7B4D-B990-A34B5D80427C" "Chapter 4 $em A Well, a Border, and a Conversation No One Should Have Had"
Set-Title "019FA96C-5EA0-7E7A-B025-CF3F824AC465" "Chapter 5 $em Five Porticoes, and the Pool They Said Wasn't There"
Set-Title "019FA96C-6EC5-7B1B-AD5A-1ABB5EAF35E9" "Chapter 6 $em Barley Loaves and a Hard Saying"
Set-Title "019FA96C-804E-712E-AAC7-D41FEF99213F" "Chapter 7 $em Water Poured at the Feast, and Brothers Who Did Not Believe"
Set-Title "019FA96C-918E-7017-B6C5-0FBFC1087903" "Chapter 8 $em The Passage That Wandered"
Set-Title "019FA96C-A291-7DFA-B5F6-1BCA319ED9C1" "Chapter 9 $em Mud, Spit, and a Man Who Kept His Story Straight"
Set-Title "019FA96C-B3A1-7AB4-B943-A964D30342D8" "Chapter 10 $em Sheepfolds, and the Feast of Dedication in Winter"
Set-Title "019FA96C-C4E0-73D8-8776-231EE5C145F8" "Chapter 11 $em Four Days"
Set-Title "019FA96C-D64E-70A6-BCC4-7027871C0FB3" "Chapter 12 $em A Pound of Nard and a Crowd with Palms"
Set-Title "019FA96C-E75D-71B3-9D66-C851C19E9A7B" "Chapter 13 $em The Basin and the Towel"
Set-Title "019FA96C-F81C-74B1-82C9-81F4FB8F94B1" "Chapter 14 $em Rooms Enough, and Another Advocate"
Set-Title "019FA96D-090A-7773-A9D3-7CDDE6929C7D" "Chapter 15 $em Pruning, and the Word 'Friend'"
Set-Title "019FA96D-19B5-756C-9B0B-ABDE952E3C34" "Chapter 16 $em A Little While"
Set-Title "019FA96D-2AAE-75B5-89EC-8C7D54E10248" "Chapter 17 $em A Prayer Overheard"
Set-Title "019FA96D-3AF3-70C2-8D25-0CB23E7F4203" "Chapter 18 $em A Lantern in a Garden and a Charcoal Fire"
Set-Title "019FA96D-4B04-7A19-B374-9FFEAD467C48" "Chapter 19 $em Three Languages on a Board"
Set-Title "019FA96D-5C2E-7177-B5DE-1F6484205004" "Chapter 20 $em The Folded Cloth and the Man Who Wanted Proof"
Set-Title "019FA96D-6C8A-712C-AB07-9F831ADF857D" "Chapter 21 $em A Hundred and Fifty-Three Fish"

$conn.Close()
Write-Host "DONE"
