# 공중 기동 기술 (Aerial Maneuver Ability)

## 개요
적이 플레이어를 추격 중일 때 발동하는 공중 기동 기술입니다. 플레이어는 수직 상승 → 덤블링 → 적 후방으로 순간 이동하는 기술을 사용할 수 있습니다.

## 기능
1. **수직 상승**: 플레이어가 공중으로 빠르게 상승
2. **덤블링 회전**: 상승 중 뒤로 한 바퀴 회전 (360도)
3. **순간 이동**: 회전 정점에서 가장 가까운 적의 후방으로 이동
4. **카메라 보정**: 기동 중에도 카메라가 부드럽게 따라오며 화면이 뒤집히지 않음

## 설치 방법

### 1. 스크립트 추가
- `AerialManeuverAbility.cs`를 Player 오브젝트에 추가
- `CameraFollow.cs`는 이미 업데이트되어 있음

### 2. 설정
Inspector에서 다음 값들을 조정할 수 있습니다:

#### 기동 기술 설정
- **Activation Key**: 발동 키 (기본: F)
- **Ascent Speed**: 수직 상승 속도 (기본: 15)
- **Ascent Height**: 상승 높이 (기본: 10)
- **Tumble Rotation Speed**: 덤블링 회전 속도 (기본: 360도/초)
- **Enemy Detection Range**: 적 감지 범위 (기본: 50)
- **Teleport Distance Behind Enemy**: 적 후방 이동 거리 (기본: 8)
- **Cooldown Time**: 쿨타임 시간 (기본: 5초)

#### Rigidbody 설정
- **Use Rigidbody**: Rigidbody 사용 여부 (기본: false)
- **Auto Add Rigidbody**: Rigidbody가 없으면 자동 추가 (기본: true)

#### 카메라 설정
- **Camera Follow**: 카메라 참조 (없으면 자동 찾기)
- **Camera Smoothness**: 기동 중 카메라 부드러움 (기본: 2)

## 사용 방법

1. **발동 조건**
   - 적이 플레이어를 추격 중이어야 함 (`EnemyController.IsTrackingPlayer()`)
   - 적이 감지 범위 내에 있어야 함
   - 쿨타임이 끝나 있어야 함

2. **키 입력**
   - 기본 키: **F** (Inspector에서 변경 가능)
   - 적 추격 중에 F 키를 누르면 발동

3. **동작 순서**
   - F 키 입력 → 수직 상승 → 덤블링 회전 → 적 후방 이동 → 안정화

## 카메라 보정 기능

`CameraFollow.cs`에 다음 기능이 추가되었습니다:

### 기동 중 카메라 처리
- `UpdateCameraRotationSmooth()`: 기동 중 카메라 회전 업데이트
- 뒤집힘 방지 강화 (최대 회전 각도 제한: 45도)
- 더 부드러운 회전 속도 적용

### 주요 보정 로직
```csharp
// 급격한 회전 방지
float maxRotationDiff = isManeuvering ? 45f : 90f;
if (rotationDiff > maxRotationDiff)
{
    float lerpFactor = isManeuvering ? 0.05f : 0.1f;
    targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpFactor);
}
```

## 코드 구조

### AerialManeuverAbility.cs
- `ExecuteAerialManeuver()`: 메인 Coroutine
- `AscentPhase()`: 수직 상승 단계
- `TumblePhase()`: 덤블링 회전 단계
- `TeleportBehindEnemy()`: 적 후방 이동 단계
- `StabilizePhase()`: 안정화 단계
- `UpdateCameraDuringManeuver()`: 기동 중 카메라 업데이트

### CameraFollow.cs 업데이트
- `UpdateCameraRotationSmooth()`: 기동 중 카메라 회전 보정
- `LateUpdate()`: 기동 중 상태 확인 및 처리

## 통합 사항

### FlightSimulationController.cs
- 기동 중에는 일반 비행 입력 무시
- `AerialManeuverAbility.IsManeuvering()` 체크 추가

## 디버깅

### GUI 표시
- 기동 중: 화면 좌측 상단에 "공중 기동 기술 실행 중..." 표시
- 쿨타임 중: 쿨타임 남은 시간 표시

### 콘솔 로그
- 기동 시작/완료 시 Debug.Log 출력
- 적을 찾을 수 없을 때 경고 메시지

## 커스터마이징

### 회전 축 변경
`TumblePhase()`에서 회전 축을 변경할 수 있습니다:
```csharp
Vector3 rotationAxis = transform.right; // 오른쪽 축 (뒤로 덤블링)
// 또는
Vector3 rotationAxis = transform.forward; // 앞쪽 축 (옆으로 회전)
```

### 이동 효과 변경
`TeleportBehindEnemy()`에서 순간 이동 효과를 조정할 수 있습니다:
```csharp
float transitionTime = 0.2f; // 더 빠르게: 0.1f, 더 느리게: 0.5f
```

## 주의사항

1. **Rigidbody 사용 시**
   - `useRigidbody = true`로 설정하면 Rigidbody를 사용합니다
   - `autoAddRigidbody = true`면 자동으로 Rigidbody가 추가됩니다
   - Rigidbody 사용 시 물리 효과가 적용됩니다

2. **행성 중심 설정**
   - Ground 오브젝트가 있어야 정확한 위치 계산이 가능합니다
   - 없으면 월드 좌표 기준으로 작동합니다

3. **적 감지**
   - `EnemyController` 컴포넌트가 있는 적만 감지됩니다
   - `IsTrackingPlayer()`가 true인 적만 대상이 됩니다

## 문제 해결

### 기동이 발동하지 않을 때
1. 적이 추격 중인지 확인 (`EnemyController.IsTrackingPlayer()`)
2. 적이 감지 범위 내에 있는지 확인
3. 쿨타임이 끝났는지 확인

### 카메라가 뒤집힐 때
1. `CameraFollow.preventFlip`이 true인지 확인
2. `minVerticalAngle`과 `maxVerticalAngle` 값 조정
3. `cameraSmoothness` 값을 높여보기

### 위치가 이상할 때
1. Ground 오브젝트가 있는지 확인
2. `planetRadius` 값이 올바른지 확인
3. 적의 Transform이 올바른지 확인
