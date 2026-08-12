# Implementation Plan — Bo Frame-Start Voice

## Goal
เติม `_frameStartClips` ใน `BoPanda_Prefab` เพื่อให้ Bo ประกาศเมื่อเริ่มเฟรมที่ 2+ ใน Chinese Pool

## Finding
`BoReferee.OnFrameStart()` มีอยู่แล้ว แต่ `_frameStartClips` ว่าง และ `BoVoicePinSetup` ตั้ง mapping เป็น empty จึงไม่มีเสียงในเฟรมถัดไป

## Change
นำ `bo_turn_start_01.wav` และ `bo_turn_start_02.wav` ที่มีอยู่แล้วมา reuse ใน `_frameStartClips` โดยไม่สร้างไฟล์เสียงใหม่ และเพิ่ม self-test ตรวจ 2 references

## Verify
- compile gate 0 errors
- Editor pin tool รันจริงและ self-test ต้องผ่าน
- ตรวจ prefab serialized array มี 2 clip references
