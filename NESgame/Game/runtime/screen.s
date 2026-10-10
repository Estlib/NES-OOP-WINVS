; ==========================================================
; NESOOP runtime - Screen
; ==========================================================


.export NESRT_Screen_SetTile

.export NESRT_Screen_SetTile_x
.export NESRT_Screen_SetTile_y
.export NESRT_Screen_SetTile_tile


; ----------------------------------------------------------
; Parameters
;
; These are written by generated NESOOP code before JSR.
; ----------------------------------------------------------

.segment "BSS"

NESRT_Screen_SetTile_x:
    .res 1

NESRT_Screen_SetTile_y:
    .res 1

NESRT_Screen_SetTile_tile:
    .res 1


; Internal temporary values

NESRT_Screen_SetTile_addrLow:
    .res 1

NESRT_Screen_SetTile_addrHigh:
    .res 1



; ----------------------------------------------------------
; Screen.SetTile(byte x, byte y, byte tile)
;
; x = 0..31
; y = 0..29
;
; Writes one 8x8 background tile into nametable $2000.
;
; IMPORTANT:
; For now this assumes rendering is OFF.
; Later this will become a queued PPU update.
; ----------------------------------------------------------

.segment "CODE"

.proc NESRT_Screen_SetTile

    ; ------------------------------------------------------
    ; Low byte:
    ;
    ; ((y & 7) * 32) + x
    ; ------------------------------------------------------

    lda NESRT_Screen_SetTile_y

    and #$07

    asl
    asl
    asl
    asl
    asl

    clc
    adc NESRT_Screen_SetTile_x

    sta NESRT_Screen_SetTile_addrLow


    ; ------------------------------------------------------
    ; High byte:
    ;
    ; $20 + (y / 8)
    ; ------------------------------------------------------

    lda NESRT_Screen_SetTile_y

    lsr
    lsr
    lsr

    clc
    adc #$20

    sta NESRT_Screen_SetTile_addrHigh


    ; ------------------------------------------------------
    ; Write PPU address
    ; ------------------------------------------------------

    lda $2002

    lda NESRT_Screen_SetTile_addrHigh
    sta $2006

    lda NESRT_Screen_SetTile_addrLow
    sta $2006


    ; ------------------------------------------------------
    ; Write tile
    ; ------------------------------------------------------

    lda NESRT_Screen_SetTile_tile
    sta $2007


    rts

.endproc