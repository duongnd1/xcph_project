# Chromatic Pour

Mở thư mục này bằng **Unity Hub → Add → Add project from disk**.
Dự án dùng Unity **2022.3.62f2**, phiên bản đọc được từ tệp Unity trong APK.
Trong Editor, mở `Assets/Scenes/Main.unity` rồi bấm **Play**. Scene đã có trong Build Settings.

Điểm khởi động là `WaterSortGame.Launch()` trong `Assets/Scripts/WaterSortGame.cs`.

| Bộ màn | Số lượng | Nhịp chơi |
| --- | ---: | --- |
| Main | 15.100 | Tăng dần số màu trong đoạn đầu, rồi xen kẽ 10 và 13 màu từ màn 101 |
| Daily | 840 | Chu kỳ 7 màn với 6, 6, 8, 8, 10, 10 và 12 màu |
| Advanced | 2.596 | Hai chặng, chặng sau bắt đầu từ màn 503 |
| Bonus | 46 | Hai nhóm 23 màn với 10 và 13 màu |

Chọn bộ màn, nhập số rồi bấm **Go**. Chọn ống nguồn rồi chọn ống đích.
Ống đích phải còn chỗ và trống hoặc có cùng màu ở trên cùng. **Undo** hoàn tác
một lượt, **Restart** đặt lại bàn hiện tại. Game lưu màn đang chọn và các màn
đã giải trên máy bằng Unity PlayerPrefs.

## Tạo lại và kiểm tra màn

Bốn tệp trong `Assets/Resources` chứa 18.582 màn được tạo mới bằng seed cố định.
Mỗi dòng chứa dung tích ống, bố cục và một chuỗi lượt rót hợp lệ để kiểm tra.
Độ dài chuỗi là độ dài một lời giải có sẵn. Số bước tối ưu và trải nghiệm thực tế
cần được kiểm tra riêng.

```powershell
node Tools/generate-levels.mjs --verify-only
node Tools/generate-levels.mjs
```

Dùng Node.js 18 trở lên. Lệnh thứ hai ghi lại bốn tệp màn từ seed cố định.

Kết quả kiểm tra lại từ các tệp đã ghi:

| Chặng | Trung vị số lượt của lời giải tạo kèm | Trung vị số bước lưu trong APK |
| --- | --- | --- |
| Main từ màn 101, nhóm 10 màu / 13 màu | 25 / 34 | 28 / 38 theo nhóm lẻ / chẵn |
| Daily theo từng vị trí trong chu kỳ | 14 / 14 / 20 / 19,5 / 25 / 25 / 30 | 15 / 15 / 21 / 21 / 28 / 28 / 34 |
| Advanced trước / từ màn 503 | 27 / 42 | 32 / 50 |
| Bonus hai nhóm 23 màn | 28 / 37 | 33 / 43 |

Trình kiểm tra đã chạy lại lời giải cho toàn bộ 18.582 màn. Bản Play Mode và
cảm giác chơi vẫn cần được kiểm tra trong Unity Editor.

## Phân tích tĩnh bản Android 5.9.0

- REA MCP xác nhận XAPK là archive ZIP. Mã bằng chứng kiểm kê là
  `ev_83023fe425cd9d88e836d76bc0bdeb7bed8cf680fe202389a489ce68ea63b47a`.
- APK chính có `AndroidManifest.xml` nhị phân. Đọc bảng chuỗi và cây XML trực tiếp
  cho thấy `com.google.firebase.OrientationUnityPlayerActivity` có cả
  `android.intent.action.MAIN` và `android.intent.category.LAUNCHER`.
- APK chính chứa `assets/bin/Data/globalgamemanagers` với chuỗi phiên bản
  `2022.3.62f2`. APK chia theo kiến trúc arm64 chứa `libunity.so` và
  `libil2cpp.so`.
- Trong `globalgamemanagers`, đường dẫn `Assets/Scenes/watersortpuzzle.unity`
  nằm sau một giá trị đếm bằng 1 và độ dài chuỗi là 35 byte. APK cũng có
  `assets/bin/Data/level0`. Hai dấu hiệu này cho thấy đó nhiều khả năng là
  scene ở build index 0. Hàm gameplay đầu tiên chưa được xác định.
- REA thiếu cấu hình JAR của JADX và launcher apktool. Phần kết quả manifest
  đến từ việc đọc trực tiếp tệp nhị phân, không phụ thuộc hai provider đó.

Mã C#, giao diện và bố cục màn chơi của Chromatic Pour được viết mới.
