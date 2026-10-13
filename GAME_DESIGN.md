# LOOP MASTER — GAMEPLAY OVERVIEW

## 1. Tổng quan

Loop Master là game giải đố sắp xếp khối màu. Người chơi lấy các khối từ những khay chứa, đưa lên băng chuyền và tận dụng cơ chế tự động hút khối vào các khay phù hợp để hoàn thành màn chơi. 

**Mục tiêu:** Hoàn thành số lượng khay theo yêu cầu của level mà không làm băng chuyền bị tắc.

## 2. Các thành phần chính

* **Tray (Khay):** Chứa các khối màu, sức chứa tối đa tương đương 4 khối lớn.
* **Block (Khối):** Có nhiều màu khác nhau. Khối lớn tách thành 10 khối nhỏ khi được đưa lên băng chuyền.
* **Conveyor (Băng chuyền):** Đường đi khép kín, di chuyển một chiều, có sức chứa giới hạn.
* **Mystery Block:** Khối có màu bị ẩn, chỉ hiện màu thật khi lên trên cùng của khay.
* **Booster:** Ba vật phẩm hỗ trợ gồm Undo, Shuffle và Expand.
* **Level:** Chứa dữ liệu khởi tạo khay, màu và thứ tự khối, sức chứa băng chuyền cùng mục tiêu màn chơi.

## 3. Luật chơi

### Khay chứa

* Các khối hoạt động theo nguyên tắc Stack (LIFO): chỉ lấy được khối trên cùng.
* Chỉ khay chưa hoàn thành mới được tương tác.
* Khay trống có thể nhận khối màu bất kỳ.
* Khay đã có khối chỉ nhận thêm khối cùng màu và còn sức chứa.
* Khi đủ 4 khối lớn cùng màu, khay hoàn thành, hiện dấu tick và bị khóa.

### Băng chuyền

* Khối di chuyển theo một chiều trên đường đi khép kín.
* Khối mới có thể chen vào giữa các khối đang di chuyển.
* Khối nhỏ đi qua miệng khay sẽ được kiểm tra để tự động hấp thụ.
* Nếu khay không phù hợp hoặc đã đầy, khối tiếp tục di chuyển.
* Băng chuyền đầy và không còn khối nào có thể được hấp thụ thì người chơi thua.

### Khối màu

* Một khối lớn tách thành 10 khối nhỏ cùng màu.
* Khối nhỏ có thể được hấp thụ từng phần vào khay.
* Sức chứa khay và băng chuyền không được vượt giới hạn.

## 4. Vật phẩm hỗ trợ

* **Undo:** Thu hồi thao tác đưa khối lên băng chuyền, khôi phục khối về khay nguồn.
* **Shuffle:** Thay đổi thứ tự khối trong một khay theo quy tắc đã thống nhất.
* **Expand:** Tăng sức chứa băng chuyền thêm 2 đơn vị, mỗi đơn vị tương đương một khối lớn.

## 5. Điều kiện thắng/thua

* **Thắng:** Hoàn thành đủ số khay mà level yêu cầu.
* **Thua:** Băng chuyền bị lấp đầy và không còn khối nào có thể được hấp thụ để giải phóng chỗ.
* Khi thắng hoặc thua, kết thúc gameplay hiện tại và hiển thị kết quả tương ứng.

## 6. Nguyên tắc bắt buộc khi lập trình

1. Dữ liệu gameplay là nguồn xác định trạng thái thực tế; hình ảnh và hoạt ảnh chỉ thể hiện trạng thái đó.
2. Mỗi khối chỉ thuộc về một vị trí logic tại một thời điểm.
3. Khi khối chuyển giữa khay và băng chuyền, phải cập nhật chính xác dữ liệu và sức chứa.
4. Không cho phép khay vượt sức chứa, nhận sai màu hoặc tương tác sau khi hoàn thành.
5. Tách dữ liệu level khỏi logic xử lý để dễ tạo và chỉnh sửa màn chơi.
6. Khi gặp luật chưa rõ, agent phải hỏi lại, không tự quyết định.
