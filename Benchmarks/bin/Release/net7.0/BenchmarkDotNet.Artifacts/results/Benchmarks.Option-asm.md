## .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```assembly
; Benchmarks.Option.CreateAndMatch_None_OneOf()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       vzeroupper
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       dword ptr [rsp+28],1
       xor       ecx,ecx
       mov       [rsp+20],rcx
       mov       byte ptr [rsp+2C],0
       vmovdqu   xmm0,xmmword ptr [rsp+20]
       vmovdqu   xmmword ptr [rsp+30],xmm0
       mov       rcx,29957806440
       mov       rsi,[rcx]
       lea       rdi,[rsp+30]
       test      rsi,rsi
       jne       short M00_L00
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rdx,29957806418
       mov       rdx,[rdx]
       lea       rcx,[rsi+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,offset Benchmarks.Option+<>c.<CreateAndMatch_None_OneOf>b__2_0(System.String)
       mov       [rsi+18],rdx
       mov       rcx,29957806440
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
M00_L00:
       mov       rcx,29957806448
       mov       rax,[rcx]
       test      rax,rax
       jne       short M00_L01
       mov       rcx,offset MT_System.Func`2[[OneOf.Types.None, OneOf],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rdx,29957806418
       mov       rdx,[rdx]
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,7FFB949708A0
       mov       [rbx+18],rdx
       mov       rcx,29957806448
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
M00_L01:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L02
       test      rsi,rsi
       je        short M00_L02
       mov       rdx,[rdi]
       mov       rcx,[rsi+8]
       call      qword ptr [rsi+18]
       jmp       short M00_L03
M00_L02:
       cmp       dword ptr [rdi+8],1
       jne       short M00_L04
       test      rax,rax
       je        short M00_L04
       movsx     rdx,byte ptr [rdi+0C]
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
M00_L03:
       nop
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L04:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       call      qword ptr [7FFB94765858]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 333
```
**Method was not JITted yet.**
Benchmarks.Option+<>c.<CreateAndMatch_None_OneOf>b__2_0(System.String)

## .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```assembly
; Benchmarks.Option.CreateAndMatch_None_UnionTypes()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       xor       ecx,ecx
       mov       [rsp+20],rcx
       mov       [rsp+28],ecx
       mov       rcx,278DA806450
       mov       rsi,[rcx]
       lea       rdi,[rsp+20]
       test      rsi,rsi
       jne       short M00_L00
       mov       rcx,offset MT_System.Func`1[[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rdx,278DA806418
       mov       rdx,[rdx]
       lea       rcx,[rsi+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,7FFB94970600
       mov       [rsi+18],rdx
       mov       rcx,278DA806450
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
M00_L00:
       mov       rcx,278DA806458
       mov       rax,[rcx]
       test      rax,rax
       jne       short M00_L01
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rdx,278DA806418
       mov       rdx,[rdx]
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,offset Benchmarks.Option+<>c.<CreateAndMatch_None_UnionTypes>b__3_1(System.String)
       mov       [rbx+18],rdx
       mov       rcx,278DA806458
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
M00_L01:
       mov       edx,[rdi+8]
       test      edx,edx
       je        short M00_L02
       cmp       edx,1
       je        short M00_L03
       jmp       short M00_L05
M00_L02:
       mov       rcx,[rsi+8]
       call      qword ptr [rsi+18]
       jmp       short M00_L06
M00_L03:
       mov       rdx,[rdi]
       mov       r8,rdx
       test      r8,r8
       je        short M00_L04
       mov       rcx,offset MT_System.String
       cmp       [r8],rcx
       jne       short M00_L07
M00_L04:
       mov       rcx,[rax+8]
       mov       rdx,r8
       call      qword ptr [rax+18]
       jmp       short M00_L06
M00_L05:
       mov       rcx,offset MD_Scifa.UnionTypes.CommonUnions.Option`1[[System.String, System.Private.CoreLib]].ThrowInvalidCaseException[[System.String, System.Private.CoreLib]](Int32)
       call      qword ptr [7FFB94971720]
M00_L06:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L07:
       call      qword ptr [7FFB9453B8B8]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       int       3
; Total bytes of code 311
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       cmp       rcx,rax
       je        short M01_L01
M01_L00:
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M01_L02
M01_L01:
       mov       rax,rdx
       ret
M01_L02:
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       jne       short M01_L00
M01_L03:
       lea       rax,[7FFBF371C548]
       jmp       qword ptr [rax]
; Total bytes of code 78
```
**Method was not JITted yet.**
Benchmarks.Option+<>c.<CreateAndMatch_None_UnionTypes>b__3_1(System.String)

## .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```assembly
; Benchmarks.Option.CreateAndMatch_Some_OneOf()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       vzeroupper
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       rcx,1FC70C091C8
       mov       rcx,[rcx]
       mov       [rsp+20],rcx
       mov       byte ptr [rsp+2C],0
       vmovdqu   xmm0,xmmword ptr [rsp+20]
       vmovdqu   xmmword ptr [rsp+30],xmm0
       mov       rcx,1FC70C06420
       mov       rsi,[rcx]
       lea       rdi,[rsp+30]
       test      rsi,rsi
       jne       short M00_L00
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rdx,1FC70C06418
       mov       rdx,[rdx]
       lea       rcx,[rsi+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,7FFB94960828
       mov       [rsi+18],rdx
       mov       rcx,1FC70C06420
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
M00_L00:
       mov       rcx,1FC70C06428
       mov       rax,[rcx]
       test      rax,rax
       jne       short M00_L01
       mov       rcx,offset MT_System.Func`2[[OneOf.Types.None, OneOf],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rdx,1FC70C06418
       mov       rdx,[rdx]
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,offset Benchmarks.Option+<>c.<CreateAndMatch_Some_OneOf>b__0_1(OneOf.Types.None)
       mov       [rbx+18],rdx
       mov       rcx,1FC70C06428
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
M00_L01:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L02
       test      rsi,rsi
       je        short M00_L02
       mov       rdx,[rdi]
       mov       rcx,[rsi+8]
       call      qword ptr [rsi+18]
       jmp       short M00_L03
M00_L02:
       cmp       dword ptr [rdi+8],1
       jne       short M00_L04
       test      rax,rax
       je        short M00_L04
       movsx     rdx,byte ptr [rdi+0C]
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
M00_L03:
       nop
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L04:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       call      qword ptr [7FFB94755858]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 336
```
**Method was not JITted yet.**
Benchmarks.Option+<>c.<CreateAndMatch_Some_OneOf>b__0_1(OneOf.Types.None)

## .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```assembly
; Benchmarks.Option.CreateAndMatch_Some_UnionTypes()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       rcx,1C5AC0091C8
       mov       rcx,[rcx]
       mov       [rsp+20],rcx
       mov       dword ptr [rsp+28],1
       mov       rcx,1C5AC006430
       mov       rsi,[rcx]
       lea       rdi,[rsp+20]
       test      rsi,rsi
       jne       short M00_L00
       mov       rcx,offset MT_System.Func`1[[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rdx,1C5AC006418
       mov       rdx,[rdx]
       lea       rcx,[rsi+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,offset Benchmarks.Option+<>c.<CreateAndMatch_Some_UnionTypes>b__1_0()
       mov       [rsi+18],rdx
       mov       rcx,1C5AC006430
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
M00_L00:
       mov       rcx,1C5AC006438
       mov       rax,[rcx]
       test      rax,rax
       jne       short M00_L01
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[System.String, System.Private.CoreLib]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rdx,1C5AC006418
       mov       rdx,[rdx]
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rdx,7FFB949608B8
       mov       [rbx+18],rdx
       mov       rcx,1C5AC006438
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,rbx
M00_L01:
       mov       edx,[rdi+8]
       test      edx,edx
       je        short M00_L02
       cmp       edx,1
       je        short M00_L03
       jmp       short M00_L05
M00_L02:
       mov       rcx,[rsi+8]
       call      qword ptr [rsi+18]
       jmp       short M00_L06
M00_L03:
       mov       rdx,[rdi]
       mov       r8,rdx
       test      r8,r8
       je        short M00_L04
       mov       rcx,offset MT_System.String
       cmp       [r8],rcx
       jne       short M00_L07
M00_L04:
       mov       rcx,[rax+8]
       mov       rdx,r8
       call      qword ptr [rax+18]
       jmp       short M00_L06
M00_L05:
       mov       rcx,offset MD_Scifa.UnionTypes.CommonUnions.Option`1[[System.String, System.Private.CoreLib]].ThrowInvalidCaseException[[System.String, System.Private.CoreLib]](Int32)
       call      qword ptr [7FFB94961A20]
M00_L06:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L07:
       call      qword ptr [7FFB9452B8B8]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       int       3
; Total bytes of code 326
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       cmp       rcx,rax
       je        short M01_L01
M01_L00:
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M01_L02
M01_L01:
       mov       rax,rdx
       ret
M01_L02:
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       jne       short M01_L00
M01_L03:
       lea       rax,[7FFBF371C548]
       jmp       qword ptr [rax]
; Total bytes of code 78
```
**Method was not JITted yet.**
Benchmarks.Option+<>c.<CreateAndMatch_Some_UnionTypes>b__1_0()

