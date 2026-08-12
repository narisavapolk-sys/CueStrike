# Implementation Plan — R43 Referee Mode UI

## Goal
ให้ผู้เล่นเลือกโหมดกรรมการจากเมนู Lobby ได้: Bo คนเดียว, ลุงคนเดียว หรือคู่กัน โดยบันทึกค่าและนำไปใช้กับ bridge เมื่อเข้าเกม

## Scope
1. `RefereeModeSwitcher` เป็น runtime service กลาง อ่าน/บันทึก PlayerPrefs และ apply mode ให้ Bo/Uncle bridges.
2. `RefereeModeUI` พร้อมปุ่ม 3 ตัวและ highlight สถานะ.
3. Editor setup ผูก UI ใน `Title_NoksGrandHall`.
4. Compile/self-test และ guard กรณี bridge โหลดช้า.

## Behavior
- Default = Bo คนเดียว (คง behavior R40)
- Bo คนเดียว: Bo enabled, Uncle disabled
- ลุงคนเดียว: Bo disabled, Uncle enabled
- คู่กัน: Bo enabled, Uncle enabled
