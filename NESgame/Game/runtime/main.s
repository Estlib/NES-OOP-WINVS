; ---------------------------------------------------------
; Minimal NES test ROM
; ---------------------------------------------------------

.segment "HEADER"

; iNES header
.byte $4E, $45, $53, $1A     ; "NES" + EOF
.byte $02                    ; 2 x 16 KB PRG ROM = 32 KB
.byte $01                    ; 1 x 8 KB CHR ROM
.byte $00                    ; mapper 0 / horizontal mirroring
.byte $00

.repeat 8
    .byte $00
.endrepeat


; ---------------------------------------------------------
; Program
; ---------------------------------------------------------

.segment "CODE"

.proc reset

    ; Disable interrupts / decimal mode
    sei
    cld

    ; Disable APU frame IRQ
    ldx #$40
    stx $4017

    ; Set up CPU stack
    ldx #$FF
    txs
    inx                     ; X = 0

    ; Disable rendering and NMI while setting things up
    stx $2000
    stx $2001

    ; Disable DMC IRQ
    stx $4010


; Wait for first vertical blank
@wait_vblank_1:
    bit $2002
    bpl @wait_vblank_1


; ---------------------------------------------------------
; Clear NES RAM
; ---------------------------------------------------------

    lda #$00

@clear_ram:

    sta $0000,x
    sta $0100,x
    sta $0200,x
    sta $0300,x
    sta $0400,x
    sta $0500,x
    sta $0600,x
    sta $0700,x

    inx
    bne @clear_ram


; Wait for another vertical blank
@wait_vblank_2:
    bit $2002
    bpl @wait_vblank_2


; ---------------------------------------------------------
; Set background colour
; ---------------------------------------------------------

    ; Reset PPU address latch
    lda $2002

    ; Address $3F00 = universal background colour
    lda #$3F
    sta $2006

    lda #$00
    sta $2006

    ; Blue-ish NES palette colour
    lda #$21
    sta $2007


; ---------------------------------------------------------
; Clear first nametable
; ---------------------------------------------------------

    lda $2002

    lda #$20
    sta $2006

    lda #$00
    sta $2006

    lda #$00
    ldx #$04
    ldy #$00

@clear_nametable:

    sta $2007

    iny
    bne @clear_nametable

    dex
    bne @clear_nametable


; ---------------------------------------------------------
; Start display
; ---------------------------------------------------------

    lda #$00
    sta $2005
    sta $2005

    ; Show background
    lda #%00001010
    sta $2001


; Sit here forever
@main_loop:
    jmp @main_loop

.endproc


; For the moment these do nothing.

.proc nmi
    rti
.endproc

.proc irq
    rti
.endproc


; ---------------------------------------------------------
; CPU vectors
; ---------------------------------------------------------

.segment "VECTORS"

.addr nmi
.addr reset
.addr irq


; ---------------------------------------------------------
; Empty 8 KB CHR ROM
; ---------------------------------------------------------

.segment "CHARS"

.res $2000, $00