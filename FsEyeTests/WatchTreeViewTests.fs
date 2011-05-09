module Test.Swensen.FsEye.Forms.WatchTreeViewTests
open Swensen.FsEye.Forms
open Swensen.Unquote
open Xunit
open Swensen.MiscUtils
open Swensen.FsEye.Model

[<Fact>]
let ``calling Watch with new name adds a node`` () =
    let tree = new WatchTreeView()
    tree.Watch("w1", 1)
    test <@ tree.Nodes.Count = 1 @>
    test <@ tree.Nodes.Find("w1", false) <> null @>

[<Fact>]
let ``calling Watch two times with different names creates two nodes`` () =
    let tree = new WatchTreeView()
    tree.Watch("w1", 1)
    tree.Watch("w2", 2)
    test <@ tree.Nodes.Count = 2 @>
    test <@ tree.Nodes.Find("w1", false).Length = 1 @>
    test <@ tree.Nodes.Find("w2", false).Length = 1 @>

[<Fact>]
let ``calling Watch with an existing name and different value replaces previous watch node`` () =
    let tree = new WatchTreeView()
    tree.Watch("w1", 1)
    tree.Watch("w1", 2)
    test <@ tree.Nodes.Count = 1 @>
    test <@ tree.Nodes.Find("w1", false).Length = 1 @>
    test <@ ((tree.Nodes.Find("w1", false).[0].Tag :?> Watch).RootInfo.Value :?> int) = 2 @>

[<Fact>]
let ``calling Watch with an existing name and same reference does nothing`` () =
    let tree = new WatchTreeView()
    let value = "hello"
    tree.Watch("w1", value)
    tree.Watch("w1", value)
    test <@ tree.Nodes.Count = 1 @>
    test <@ tree.Nodes.Find("w1", false).Length = 1 @>
    test <@ ((tree.Nodes.Find("w1", false).[0].Tag :?> Watch).RootInfo.Value :?> string) =& value @>
