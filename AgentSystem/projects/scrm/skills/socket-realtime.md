# Recipe — Socket realtime (SignalR)

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Push cập nhật real-time xuống FE (trạng thái đơn, tiến độ batch, thông báo) mà không cần FE polling.

## Thư viện
- **BE:** `Microsoft.AspNetCore.SignalR` + Redis backplane (scale nhiều instance).
- **FE:** `@microsoft/signalr` (`HubConnectionBuilder`) + RxJS `BehaviorSubject`.

## BE — emit event
```csharp
public class ProcessEcommerceHub : Hub { }   // Hub

// emit tới group (vd theo đơn vị/user)
await _hubContext.Clients.Group(groupName).SendAsync("EventName", payload);
// thường gọi từ controller/service sau khi có thay đổi
```

## FE — connect & lắng nghe
```typescript
this.connection = new signalR.HubConnectionBuilder()
  .withUrl(`${environment.SOCKET_URL}/hub`, { withCredentials: true })
  .withAutomaticReconnect()
  .build();
await this.connection.start();
this.connection.on('EventName', (payload) => { this.subject.next(payload); });   // đẩy vào BehaviorSubject
this.connection.onreconnected(() => { /* re-join group nếu cần */ });

// component:
this.socketService.stream$.subscribe(data => { /* cập nhật UI */ });
ngOnDestroy() { this.sub?.unsubscribe(); this.connection?.stop(); }   // cleanup
```

## Bẫy thường gặp
- **Reconnect:** `withAutomaticReconnect()` + `onreconnected` re-join group (mất group khi đứt kết nối).
- **Memory leak:** unsubscribe + `connection.stop()` trong `ngOnDestroy`.
- **Scope group** theo user/đơn vị để không push nhầm cross-tenant.
- Nhiều instance BE → cần **Redis backplane** mới broadcast đúng.
- `withCredentials: true` để gửi cookie/auth.

## File source mẫu
- BE: `ProcessEcommerceHub.cs` (Clients.Group + SendAsync) · controller gọi notify
- FE: `*SocketService.ts` (HubConnectionBuilder + reconnect) · component subscribe + ngOnDestroy
