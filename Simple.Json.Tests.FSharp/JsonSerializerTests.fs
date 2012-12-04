module Simple.Json.Tests.FSharp.JsonSerializerTests


open Xunit

let shouldEqual<'a> (x : 'a) (y : 'a) = Assert.Equal<'a> (x, y)


open Simple.Json

type optional<'a> = Optional<'a>
let undefined = optional<_> ()
let specified x = optional<_> x
let toJson<'a> (x : 'a) = JsonSerializer.Default.ToJson (x, typeof<'a>, false)
let fromJson<'a> (x : string) : 'a = downcast JsonSerializer.Default.ParseJson (x, typeof<'a>)



type Point = { x : float; y : float; z : float optional }



[<Fact>]
let ``Can serialize list`` () = 

    toJson [1; 2; 3] 
    |> shouldEqual "[1,2,3]"



[<Fact>]
let ``Can deserialize list`` () = 

    fromJson "[1,2,3]" 
    |> shouldEqual [1; 2; 3]


[<Fact>]
let ``Can serialize record type`` () = 

    toJson { x = 12.0; y = 45.0; z = undefined } 
    |> shouldEqual "{\"x\":12,\"y\":45}"
    
    toJson { x = 12.0; y = 45.0; z = specified -100.0 } 
    |> shouldEqual "{\"x\":12,\"y\":45,\"z\":-100}"



[<Fact>]
let ``Can deserialize record type`` () = 

    fromJson "{\"x\":12,\"y\":45}" 
    |> shouldEqual { x = 12.0; y = 45.0; z = undefined }
    
    fromJson "{\"x\":12,\"y\":45,\"z\":-100}" 
    |> shouldEqual { x = 12.0; y = 45.0; z = specified -100.0 }
    
    
