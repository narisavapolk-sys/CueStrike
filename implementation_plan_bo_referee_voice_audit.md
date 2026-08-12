# Implementation Plan — Bo Referee Voice Audit

## Goal
เก็บ PlayMode audit ถาวรสำหรับ Bo กรรมการ R40/R42 ให้ตรวจฉาก AAA_RoomDAY จริง ทั้ง wiring, voice clips และ event announcement path

## Checks
1. โหลด AAA_RoomDAY และตรวจ BoReferee, BoRefereeEventBridge, AudioSource และ voice clips อย่างน้อย 14 คลิป
2. ตรวจ bridge subscribe กับ Chinese Pool events
3. เรียก event-facing API ของ Bo สำหรับ match start, foul และ ball potted พร้อมตรวจ announcement timestamp และ Animator state/transition

## Constraints
- ใช้ reflection เพราะ runtime assembly ตั้ง `autoReferenced: false`
- ไม่แก้พฤติกรรมเกมหรือ prefab
- ใช้ PlayMode runner จริง และเก็บ console evidence ใน CI
