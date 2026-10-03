## Fees Module API Endpoints

This document summarizes all endpoints related to the **fees module** in the BonzerSchool ERP, including fee configuration, invoicing, payments, and analytics/dashboards.

All endpoints are under the base URL pattern:

- `https://{host}/api/{Controller}/{actionRoute?}`
- All fees endpoints are secured with `[Authorize]`.

---

## 1. Fee Heads (`FeeHeadController`)

Manage **types of fees** (e.g. Tuition, Transport, Library).

### `GET /api/FeeHead`
- **Purpose**: List all fee heads.
- **Request**: No body.
- **Response**: `FeeHeadResponse[]`

```json
{
  "id": "guid",
  "name": "string",
  "accountCode": "string"
}
```

---

### `GET /api/FeeHead/{id}`
- **Purpose**: Get a single fee head by its Id.
- **Path params**:
  - `id` (Guid) – fee head Id.
- **Response**: `FeeHeadResponse` (as above) or `404` if not found.

---

### `POST /api/FeeHead`
- **Purpose**: Create a new fee head.
- **Request body**: `FeeHeadRequest`

```json
{
  "name": "string",
  "accountCode": "string"
}
```

- **Response**: `201 Created` with `FeeHeadResponse`.

---

### `PUT /api/FeeHead/{id}`
- **Purpose**: Update an existing fee head.
- **Path params**:
  - `id` (Guid) – fee head Id.
- **Request body**: `FeeHeadRequest` (same as above).
- **Response**: `200 OK` with updated `FeeHeadResponse`.

---

### `DELETE /api/FeeHead/{id}`
- **Purpose**: Delete a fee head.
- **Path params**:
  - `id` (Guid) – fee head Id.
- **Response**:
  - `204 No Content` on success.
  - `404 Not Found` if not found.

---

## 2. Fee Structures (`FeeStructureController`)

Configure **fee amount per class/division per head**.

### `GET /api/FeeStructure`
- **Purpose**: List all fee structures.
- **Response**: `FeeStructureResponse[]`

```json
{
  "id": "guid",
  "classId": "guid",
  "divisionId": "guid",
  "feeHeadId": "guid",
  "amount": 1000.0
}
```

---

### `GET /api/FeeStructure/{id}`
- **Purpose**: Get a single fee structure record.
- **Path params**: `id` (Guid).
- **Response**: `FeeStructureResponse` or `404`.

---

### `POST /api/FeeStructure`
- **Purpose**: Create a fee structure (link class/division + fee head + amount).
- **Request body**: `FeeStructureRequest`

```json
{
  "classId": "guid",
  "divisionId": "guid",
  "feeHeadId": "guid",
  "amount": 1000.0
}
```

- **Response**: `201 Created` with `FeeStructureResponse`.

---

### `PUT /api/FeeStructure/{id}`
- **Purpose**: Update fee structure.
- **Path params**: `id` (Guid).
- **Request body**: `FeeStructureRequest` (same as above).
- **Response**: `200 OK` with updated `FeeStructureResponse`.

---

### `DELETE /api/FeeStructure/{id}`
- **Purpose**: Delete fee structure.
- **Path params**: `id` (Guid).
- **Response**: `204 No Content` or `404 Not Found`.

---

## 3. Class Fee Map (`ClassFeeMapController`)

Map **fee heads and amounts to class/division/section and academic year** (more granular configuration).

### `GET /api/ClassFeeMap`
- **Purpose**: List all class–fee mappings.
- **Response**: `ClassFeeMapResponse[]`

```json
{
  "id": "guid",
  "classId": "guid",
  "divisionId": "guid",
  "sectionId": "guid",
  "academicYearId": "guid",
  "feeHeadId": "guid",
  "amount": 1200.0,
  "installmentNo": 1,
  "academicYear": "2025-26",
  "createdAt": "2026-03-10T00:00:00Z"
}
```

---

### `GET /api/ClassFeeMap/{id}`
- **Purpose**: Get single mapping by Id.
- **Path params**: `id` (Guid).
- **Response**: `ClassFeeMapResponse` or `404`.

---

### `POST /api/ClassFeeMap`
- **Purpose**: Create class–fee mapping.
- **Request body**: `ClassFeeMapRequest`

```json
{
  "classId": "guid",
  "divisionId": "guid",
  "sectionId": "guid",
  "academicYearId": "guid",
  "feeHeadId": "guid",
  "amount": 1200.0,
  "installmentNo": 1,
  "academicYear": "2025-26",
  "createdAt": "2026-03-10T00:00:00Z"
}
```

- **Response**: `201 Created` with `ClassFeeMapResponse`.

---

### `PUT /api/ClassFeeMap/{id}`
- **Purpose**: Update mapping.
- **Path params**: `id` (Guid).
- **Request body**: `ClassFeeMapRequest`.
- **Response**: `200 OK` with updated `ClassFeeMapResponse`.

---

### `DELETE /api/ClassFeeMap/{id}`
- **Purpose**: Delete mapping.
- **Response**: `204 No Content` or `404 Not Found`.

---

## 4. Fee Installments (`FeeInstallmentController`)

Define **installment schedule** (per branch/class/year).

### `GET /api/FeeInstallment`
- **Purpose**: List all fee installment definitions.
- **Response**: `FeeInstallmentResponse[]`

```json
{
  "id": "guid",
  "branchId": "guid",
  "classId": "guid",
  "academicYearId": "guid",
  "installmentNo": 1,
  "installmentName": "First Term",
  "dueDate": "2026-06-15",
  "lateFeeAfter": "2026-06-20",
  "createdAt": "2026-03-10T00:00:00Z"
}
```

---

### `GET /api/FeeInstallment/{id}`
- **Purpose**: Get a single installment definition.
- **Path params**: `id` (Guid).
- **Response**: `FeeInstallmentResponse` or `404`.

---

### `POST /api/FeeInstallment`
- **Purpose**: Create/install a new installment schedule entry.
- **Request body**: `FeeInstallmentRequest`

```json
{
  "branchId": "guid",
  "classId": "guid",
  "academicYearId": "guid",
  "installmentNo": 1,
  "installmentName": "First Term",
  "dueDate": "2026-06-15",
  "lateFeeAfter": "2026-06-20",
  "createdAt": "2026-03-10T00:00:00Z"
}
```

- **Response**: `201 Created` with `FeeInstallmentResponse`.

---

### `PUT /api/FeeInstallment/{id}`
- **Purpose**: Update installment definition.
- **Path params**: `id` (Guid).
- **Request body**: `FeeInstallmentRequest`.
- **Response**: `200 OK` with `FeeInstallmentResponse`.

---

### `DELETE /api/FeeInstallment/{id}`
- **Purpose**: Delete installment.
- **Response**: `204 No Content` or `404`.

---

## 5. Invoices (`InvoiceController`) and Fee Head Distribution

Represents **student fee invoices** (per student/installment/month).  
Each invoice is further broken into **fee-head level items** in the `invoice_items` table (one row per fee head per invoice, with `TotalAmount`, `PaidAmount`, `RemainingAmount`).  
This enables analytics such as **“how many students have paid Annual Function fee”** by student/class/division/branch.

### `GET /api/Invoice`
- **Purpose**: List all invoices (header-level information).
- **Response**: `InvoiceResponse[]`

```json
{
  "id": "guid",
  "studentId": "guid",
  "branchId": "guid",
  "classId": "guid",
  "divisionId": "guid",
  "installmentId": "guid",
  "nextInstallmentDate": "2026-07-15",
  "invoiceNo": "INV-2026-0001",
  "invoiceDate": "2026-04-01",
  "totalAmount": 15000.0,
  "paidAmount": 9000.0,
  "remainingAmount": 6000.0,
  "dueDate": "2026-04-30",
  "status": "PARTIAL",
  "month": 4,
  "year": 2026,
  "createdAt": "2026-04-01T00:00:00Z"
}
```

> **Fee head distribution for a student** is obtained from `invoice_items`:
> - Filter `invoice_items` by `invoice_id` (for that student) and `fee_head_id` (e.g. “Annual Function” head).
> - Sum `TotalAmount`, `PaidAmount`, `RemainingAmount` to see **what type of fees have been paid** (e.g. only `Gathering` or `Picnic` fees).
> - Aggregations at **student / class / division / branch** level are exposed via analytics endpoints under `FeesController` (see `feehead-analytics` and `outstanding-*` for building dashboards).

---

### `GET /api/Invoice/{id}`
- **Purpose**: Get one invoice.
- **Path params**: `id` (Guid).
- **Response**: `InvoiceResponse` or `404`.

---

### `POST /api/Invoice`
- **Purpose**: Create a new invoice.
- **Request body**: `InvoiceRequest` (same shape as `InvoiceResponse` minus server fields).

```json
{
  "id": "guid",             // optional, usually new guid
  "studentId": "guid",
  "branchId": "guid",
  "classId": "guid",
  "divisionId": "guid",
  "installmentId": "guid",
  "nextInstallmentDate": "2026-07-15",
  "invoiceNo": "INV-2026-0001",
  "invoiceDate": "2026-04-01",
  "totalAmount": 15000.0,
  "paidAmount": 0.0,
  "remainingAmount": 15000.0,
  "dueDate": "2026-04-30",
  "status": "PENDING",
  "month": 4,
  "year": 2026,
  "createdAt": "2026-04-01T00:00:00Z"
}
```

- **Response**: `201 Created` with `InvoiceResponse`.

---

### `PUT /api/Invoice/{id}`
- **Purpose**: Update invoice (e.g. amounts or status).
- **Path params**: `id` (Guid).
- **Request body**: `InvoiceRequest`.
- **Response**: `200 OK` with updated `InvoiceResponse`.

---

### `DELETE /api/Invoice/{id}`
- **Purpose**: Delete invoice.
- **Response**: `204 No Content` or `404`.

---

## 6. Payments (`PaymentController`)

Represents **payments/receipts** against invoices/installments.

> Note: Responses are wrapped in `ApiResponse<object>` – the inner value is typically a `PaymentResponse` or list.

> **Database:** The receipt endpoint (`GET /api/Payment/{id}/receipt`) and slip print use the **payment_fee_lines** table to store per-payment fee head breakdown. If your schema does not have this table yet, create it (e.g. run the following SQL once):
>
> ```sql
> CREATE TABLE IF NOT EXISTS payment_fee_lines (
>     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
>     payment_id UUID NOT NULL REFERENCES payments(id),
>     fee_head_id UUID NOT NULL REFERENCES fee_heads(id),
>     amount NUMERIC(10,2) NOT NULL
> );
> ```

### `GET /api/Payment`
- **Purpose**: List all payments.
- **Response**: `ApiResponse<PaymentResponse[]>`

```json
{
  "data": [
    {
      "id": "guid",
      "invoiceId": "guid",
      "studentId": "guid",
      "branchId": "guid",
      "divisionId": "guid",
      "installmentId": "guid",
      "collectedByStaffId": "guid",
      "paymentDate": "2026-04-05",
      "amount": 5000.0,
      "paymentMethod": "CASH",
      "paymentType": "FEE",
      "transactionNumber": "TXN-123",
      "bankName": "ABC Bank",
      "remarks": "First installment",
      "createdAt": "2026-04-05T00:00:00Z"
    }
  ]
}
```

---

### `GET /api/Payment/{id}`
- **Purpose**: Get one payment by Id.
- **Path params**: `id` (Guid).
- **Response**: `ApiResponse<PaymentResponse>`.

---

### `GET /api/Payment/{id}/receipt`
- **Purpose**: Get **receipt/slip data for this payment** (for print or re-print). **One slip per payment** – every time a student makes a payment, the accountant can print a new slip; this endpoint returns the data for that slip.
- **Path params**: `id` (Guid) – payment Id.
- **Response**: `ApiResponse<PaymentReceiptDto>` or `404` if payment not found.

```json
{
  "data": {
    "paymentId": "guid",
    "paymentDate": "2026-04-05",
    "amount": 500.0,
    "paymentMethod": "CASH",
    "transactionNumber": "TXN-123",
    "bankName": null,
    "remarks": "Picnic fee",
    "createdAt": "2026-04-05T10:30:00Z",
    "rollNumber": "12",
    "classId": "guid",
    "divisionId": "guid",
    "branchName": "Main Branch",
    "feeLines": [
      { "feeHeadName": "Picnic", "amount": 300.0 },
      { "feeHeadName": "Annual Function", "amount": 200.0 }
    ]
  }
}
```

- **Usage**: After `POST /api/Payment` succeeds, call `GET /api/Payment/{response.id}/receipt` and use the response to render and print the payment slip (browser print or PDF). The slip identifies the student by **roll number**, **class id**, and **division id**. You can call this same endpoint later to **re-print** the same receipt.

---

### `GET /api/Payment/{id}/receipt/pdf`
- **Purpose**: Get the **receipt as a PDF file**. Use so the accountant can **share the receipt** or the **user (student/parent) can view or download the PDF**. In the **payment history** section, each payment entry can show a “View PDF” / “Download PDF” link that calls this endpoint.
- **Path params**: `id` (Guid) – payment Id.
- **Query params**:
  - `disposition` (optional): `inline` (default) – open PDF in browser; `attachment` – download as file.
- **Response**: PDF binary (`Content-Type: application/pdf`), or `404` if payment not found.

| Scenario | URL example | Behavior |
|----------|-------------|----------|
| View in browser | `GET /api/Payment/{id}/receipt/pdf` or `?disposition=inline` | PDF opens in same tab / new tab (e.g. payment history “View PDF”). |
| Download file | `GET /api/Payment/{id}/receipt/pdf?disposition=attachment` | Browser downloads `receipt-{id}.pdf`. |

- **Payment history UI**: List payments (e.g. via `GET /api/Payment` filtered by student, or from student dashboard). For each row, add a “View PDF” button/link that opens `GET /api/Payment/{paymentId}/receipt/pdf` (inline) or “Download PDF” that uses `?disposition=attachment`.

---

### `POST /api/Payment`
- **Purpose**: Record a new payment.
- **Request body**: `PaymentRequest` (now supports fee-head wise lines)

```json
{
  "invoiceId": "guid",
  "studentId": "guid",
  "branchId": "guid",
  "divisionId": "guid",
  "installmentId": "guid",
  "collectedByStaffId": "guid",
  "paymentDate": "2026-04-05",
  "amount": 5000.0,
  "paymentMethod": "CASH",
  "paymentType": "FEE",
  "transactionNumber": "TXN-123",
  "bankName": "ABC Bank",
  "remarks": "Picnic + Gathering",
  "createdAt": "2026-04-05T00:00:00Z",
  "feeLines": [
    {
      "feeHeadId": "GUID_OF_PICNIC_FEEHEAD",
      "amount": 300.0
    },
    {
      "feeHeadId": "GUID_OF_GATHERING_FEEHEAD",
      "amount": 200.0
    }
  ]
}
```

- **Response**: `ApiResponse<PaymentResponse>`.

---

### `PUT /api/Payment/{id}`
- **Purpose**: Update existing payment (rare).
- **Path params**: `id` (Guid).
- **Request body**: `PaymentRequest`.
- **Response**: `ApiResponse<PaymentResponse>`.

---

### `DELETE /api/Payment/{id}` 
- **Purpose**: Delete payment (e.g. void/rollback).
- **Response**: `ApiResponse<bool>` or similar flag.

---

## 7. Fees Analytics & Dashboards (`Fees Controller`)

High-level analytics (suitable for graphs, pie charts, dashboards).

**Student identification:** The fees module uses **roll number** (and optionally branch/class/division) to identify students in responses and in the dashboard-by-roll endpoint. DTOs expose `rollNumber` instead of student name where a student identifier is needed.

### `GET /api/Fees/student-dashboard`
- **Purpose**: Student-wise fee summary (by student Id) for an academic year.
- **Query params**:
  - `studentId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `StudentFeeDashboardDto` (includes `rollNumber` when available).

```json
{
  "studentId": "guid",
  "rollNumber": "12",
  "totalFees": 15000.0,
  "paidFees": 9000.0,
  "remainingFees": 6000.0,
  "feeHeads": [
    {
      "feeHeadId": "guid",
      "feeHeadName": "Tuition",
      "totalAmount": 10000.0,
      "paidAmount": 6000.0,
      "remainingAmount": 4000.0
    }
  ],
  "installments": [
    {
      "installmentId": "guid",
      "installmentName": "First Term",
      "totalAmount": 7500.0,
      "paidAmount": 4500.0,
      "remainingAmount": 3000.0,
      "dueDate": "2026-06-15T00:00:00"
    }
  ]
}
```

---

### `GET /api/Fees/student-dashboard-by-roll`
- **Purpose**: Same as `student-dashboard`, but filtered by **branch + class + roll number**.
- **Query params**:
  - `branchId` (Guid)
  - `classId` (Guid)
  - `rollNumber` (string)
  - `academicYearId` (Guid)
- **Response**: `StudentFeeDashboardDto` (as above).

---

### `GET /api/Fees/class-dashboard`
- **Purpose**: Class-wise collection summary (for cards/graphs).
- **Query params**:
  - `branchId` (Guid)
  - `classId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `ClassFeeDashboardDto`

```json
{
  "classId": "guid",
  "totalFees": 500000.0,
  "collectedFees": 380000.0,
  "pendingFees": 120000.0,
  "totalStudents": 60,
  "collectionRate": 76.0
}
```

---

### `GET /api/Fees/division-dashboard`
- **Purpose**: Division-wise collection summary.
- **Query params**:
  - `branchId` (Guid)
  - `divisionId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `DivisionFeeDashboardDto` (similar shape to class).

---

### `GET /api/Fees/branch-dashboard`
- **Purpose**: Branch-wise collection summary.
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `BranchFeeDashboardDto`.

---

### `GET /api/Fees/collection-trend`
- **Purpose**: Date-wise **collection trend** for a branch (for line charts).
- **Query params**:
  - `branchId` (Guid)
  - `startDate` (DateTime)
  - `endDate` (DateTime)
- **Response**: `FeeCollectionTrendDto[]`

```json
[
  { "date": "2026-03-01T00:00:00", "amount": 12000.0 },
  { "date": "2026-03-02T00:00:00", "amount": 8000.0 }
]
```

---

### `GET /api/Fees/feehead-analytics`
- **Purpose**: Branch + year-wise **expected amount per fee head** (for pie/bar charts of fee type distribution).
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `FeeHeadAnalyticsDto[]`

```json
[
  {
    "feeHeadId": "guid",
    "feeHeadName": "Tuition",
    "totalAmount": 400000.0
  }
]
```

---

### `GET /api/Fees/installment-analytics`
- **Purpose**: Branch + year-wise **expected vs collected per installment**.
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `InstallmentCollectionDto[]`

```json
[
  {
    "installmentId": "guid",
    "installmentName": "First Term",
    "expectedAmount": 250000.0,
    "collectedAmount": 200000.0
  }
]
```

Use this to compute **remaining** per installment (`expected - collected`) for charts.

---

### `GET /api/Fees/outstanding-by-student`
- **Purpose**: List students with **outstanding fees**, optionally filtered by class/division, for a branch + academic year.
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
  - `classId` (Guid, optional)
  - `divisionId` (Guid, optional)
- **Response**: `StudentOutstandingFeeDto[]`

```json
[
  {
    "studentId": "guid",
    "rollNumber": "12",
    "classId": "guid",
    "divisionId": "guid",
    "remainingAmount": 6000.0
  }
]
```

---

### `GET /api/Fees/outstanding-by-class`
- **Purpose**: Class-wise total outstanding fees (for bar/pie charts).
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `ClassOutstandingFeeDto[]`

```json
[
  {
    "classId": "guid",
    "className": "Std 5",
    "remainingAmount": 120000.0
  }
]
```

---

### `GET /api/Fees/outstanding-by-division`
- **Purpose**: Division-wise total outstanding fees.
- **Query params**:
  - `branchId` (Guid)
  - `academicYearId` (Guid)
- **Response**: `DivisionOutstandingFeeDto[]`

```json
[
  {
    "divisionId": "guid",
    "divisionName": "A",
    "remainingAmount": 50000.0
  }
]
```

---

### `GET /api/Fees/outstanding-by-branch`
- **Purpose**: Branch-wise total outstanding fees (multi-branch overview).
- **Query params**:
  - `academicYearId` (Guid)
- **Response**: `BranchOutstandingFeeDto[]`

```json
[
  {
    "branchId": "guid",
    "branchName": "Main Campus",
    "remainingAmount": 300000.0
  }
]
```

---

### `GET /api/Fees/feehead-students`
- **Purpose**: For a **specific fee head** (e.g. Picnic, Annual Function), get **per-student totals** (total/paid/remaining) within a branch/academic year, optionally filtered by class/division.
- **Query params**:
  - `feeHeadId` (Guid) – e.g. FeeHead for “Picnic Fee” or “Annual Function Fee”.
  - `branchId` (Guid)
  - `academicYearId` (Guid)
  - `classId` (Guid, optional)
  - `divisionId` (Guid, optional)
- **Response**: `StudentFeeHeadStatusDto[]`

```json
[
  {
    "studentId": "guid",
    "rollNumber": "12",
    "classId": "guid",
    "divisionId": "guid",
    "feeHeadId": "guid",
    "feeHeadName": "Picnic Fee",
    "totalAmount": 500.0,
    "paidAmount": 500.0,
    "remainingAmount": 0.0
  }
]
```

- **Usage examples**:
  - **Picnic fee in a particular division**: call with `feeHeadId = PicnicFeeHeadId`, `branchId`, `academicYearId`, `divisionId = that division`, and count how many entries have `paidAmount >= totalAmount` (fully paid) vs `remainingAmount > 0` (not fully paid).
  - **Annual function fee per class**: call with `feeHeadId = AnnualFunctionHeadId`, `branchId`, `academicYearId`, `classId = that class` and build bar/pie charts of paid vs pending students.

---

## 8. Students with Outstanding Fees (`StudentController`)

From student module but tightly related to fees.

### `GET /api/Student/with-outstanding-fees`
- **Purpose**: Get list of students who currently have **any outstanding fees**.
- **Request**: No body, no extra query parameters.
- **Response**: `StudentResponse[]` (student DTO used elsewhere).
- **Usage**: For a quick “defaulters list” without full monetary breakdown.

---

This `.md` file is intended as **reference** for frontend developers and integrators building dashboards, reports, and UI around the fees module. It covers:

- CRUD endpoints for fee heads, structures, class mappings, installments.
- Invoices and payments.
- Aggregated analytics (student/class/division/branch, fee type, installments, trends).

---

## 9. End-to-end Fees Flow (From Student Payment to Receipt + Notification)

This section explains the **full runtime flow** when a student comes to pay fees, from reading dues to printing and sending the receipt by email/WhatsApp.

### 9.1 High-level flow

1. **Cashier identifies student**
   - UI uses branch/class/roll number (or student Id).
   - Calls `GET /api/Fees/student-dashboard-by-roll` (or `student-dashboard`).
   - Shows total fees, paid, remaining, and per-fee-head + per-installment breakdown.

2. **Invoice context**
   - Either invoices are **pre-generated** (recommended) or:
   - System creates invoice on demand with `POST /api/Invoice` and `InvoiceService` splits total into `invoice_items` (one per `FeeHeadId`).

3. **Cashier selects what to pay now**
   - From `feeHeads[]` the UI lets cashier choose fee heads (e.g. only Picnic, Picnic + Annual Function) and specify amounts.

4. **Record payment fee-head wise**
   - UI calls `POST /api/Payment` with `PaymentRequest` including `feeLines[]`:
     - Each line: `{ "feeHeadId": ..., "amount": ... }`.
   - `PaymentService`:
     - Validates `Amount == sum(feeLines.amount)` (or auto-sets Amount).
     - Saves `Payment`.
     - Calls `ApplyPaymentToInvoiceItems` which:
       - Finds `invoice_items` for that `invoiceId` and `feeHeadId`.
       - Updates `PaidAmount` / `RemainingAmount` per item.
       - Recomputes invoice header totals from all items.

5. **Refresh and print receipt**
   - UI calls `GET /api/Fees/student-dashboard(-by-roll)` again.
   - Uses `StudentFeeDashboardDto`:
     - `FeeHeads[]` → print which fee heads were paid and what remains.
     - `Installments[]` → print installment-wise view.
   - UI renders and prints a PDF/HTML receipt for the cashier.

6. **Send soft copy via email/WhatsApp**
   - UI or backend generates a receipt link/HTML.
   - Calls `POST /api/Notification` with `NotificationRequest`:
     - `type` = `"FEE_RECEIPT_EMAIL"` or `"FEE_RECEIPT_WHATSAPP"`.
     - `studentId` (to resolve email/phone from `Student` profile).
     - `title`/`message` containing summary or link to receipt.
   - `NotificationService` persists the notification and (when wired) invokes the actual email/WhatsApp provider based on `Type`.

### 9.2 Flow diagram (simplified)

```text
Cashier UI
   |
   | 1. Identify student
   |--> GET /api/Fees/student-dashboard-by-roll
   |<-- StudentFeeDashboardDto (feeHeads + installments)
   |
   | 2. Ensure invoice exists (optional, if not pre-generated)
   |--> POST /api/Invoice
   |    InvoiceService -> CreateInvoiceItemsForInvoice
   |
   | 3. Cashier selects fee heads + amounts
   |
   | 4. Record payment
   |--> POST /api/Payment (PaymentRequest + feeLines[])
   |    PaymentService:
   |       - Save Payment
   |       - ApplyPaymentToInvoiceItems:
   |           invoice_items (per FeeHead)
   |           invoices (header totals)
   |
   | 5. Print receipt (one slip per payment)
   |--> GET /api/Payment/{paymentId}/receipt
   |<-- PaymentReceiptDto (rollNumber, classId, divisionId, amount, fee lines)
   |    → UI renders slip and triggers print (or PDF)
   |
   | 5b. Optional: refresh dashboard for balance
   |--> GET /api/Fees/student-dashboard(-by-roll)
   |<-- Updated StudentFeeDashboardDto
   |
   | 6. Send notifications
   |--> POST /api/Notification (type FEE_RECEIPT_EMAIL / FEE_RECEIPT_WHATSAPP)
        NotificationService:
           - resolve contact info (student/father numbers, email)
           - send via configured providers (email / WhatsApp)
```

In this design:

- **Fee configuration** (`FeeHead`, `FeeStructure`, `ClassFeeMap`, `FeeInstallment`) defines what should be charged.
- **Invoices + `invoice_items`** define what has been billed and at which fee head.
- **Payments with `feeLines`** define exactly which fee heads have been paid and how much.
- **Dashboards (`FeesController`)** and **notifications (`NotificationController`)** use that data to present analytics, print receipts, and send digital copies.

### 9.3 How payment slip (receipt) print works

- **One payment → one new slip.** When a student pays, the accountant records the payment with `POST /api/Payment` (with `invoiceId`, `amount`, `feeLines[]`). Every time a **new** payment is made, you get a **new** slip to print.
- **Print flow:**
  1. Cashier records payment → `POST /api/Payment` → response includes `id` (payment Id).
  2. Frontend calls `GET /api/Payment/{id}/receipt` to get slip data (roll number, class id, division id, amount, fee head breakdown, date, etc.).
  3. UI renders a receipt (e.g. “Payment Receipt”, receipt no = payment Id, roll number, class id, division id, date, amount, fee lines) and triggers **print** (browser print dialog or PDF).
- **Re-print:** The same slip can be printed again anytime by calling `GET /api/Payment/{id}/receipt` for that payment Id. Fee line breakdown is stored per payment in `payment_fee_lines` when the payment is created.

### 9.4 Student can pay any fees at any time

- Students are **not** required to pay in a fixed order or only one fee type at a time. The cashier can:
  - Look up dues with `GET /api/Fees/student-dashboard-by-roll` (or `student-dashboard`).
  - Choose **which fee heads** to pay in this transaction (e.g. only Picnic, or Picnic + Tuition + Transport).
  - Send `POST /api/Payment` with `feeLines[]` where each line is a `feeHeadId` and `amount`.
- The system applies the payment to the correct fee heads in `invoice_items` and stores the breakdown in `payment_fee_lines` so the printed slip shows exactly what was paid in that transaction.

### 9.5 Receipt PDF, share, and payment history

- **After print or payment**, the accountant can **share the receipt** and the **user (student/parent) can see or download a PDF**:
  - **Share:** Send the link `GET /api/Payment/{id}/receipt/pdf` (e.g. by email or WhatsApp via `POST /api/Notification`) or open it to show the PDF in the browser.
  - **User sees PDF:** In the app, provide a “View receipt” or “Download PDF” action that calls the same endpoint with `disposition=inline` (view in browser) or `disposition=attachment` (download).
- **Payment history section:** When the user views their payment history (list of payments for a student), **each entry can show a PDF**:
  - List payments (e.g. filter `GET /api/Payment` by student, or use fees dashboard data).
  - For each payment row, add a “View PDF” or “Download PDF” button that calls `GET /api/Payment/{paymentId}/receipt/pdf` (with `disposition=inline` to open in a new tab, or `attachment` to download). Thus the user can see the PDF for every payment in the history. 

