# Remaining Fees API - Usage Examples

## Base URL
```
http://localhost:5000/api/remainingfees
```

## Example Requests

### 1. Get Student Remaining Fees
**Endpoint:** `GET /api/remainingfees/student/{studentId}`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/student/55836f20-2f9d-4755-b64c-c755741f05a9" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### C# HttpClient Example
```csharp
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
    
    var studentId = new Guid("55836f20-2f9d-4755-b64c-c755741f05a9");
    var response = await client.GetAsync($"http://localhost:5000/api/remainingfees/student/{studentId}");
    
    if (response.IsSuccessStatusCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RemainingFeesResponse>(json);
        Console.WriteLine($"Remaining Fees: {result.Data.RemainingFees}");
    }
}
```

#### Example Response
```json
{
  "success": true,
  "message": "Student remaining fees retrieved successfully",
  "data": {
    "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
    "studentName": "John Doe",
    "admissionNo": "ADM-001",
    "classId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "divisionId": "d4c3b2a1-f6e5-0987-dcba-09876543210f",
    "branchId": "12345678-1234-5678-1234-567812345678",
    "totalDueFees": 50000.00,
    "paidAmount": 30000.00,
    "remainingFees": 20000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 2. Get Student Fee Details (By Fee Head)
**Endpoint:** `GET /api/remainingfees/student/{studentId}/details`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/student/55836f20-2f9d-4755-b64c-c755741f05a9/details" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### C# HttpClient Example
```csharp
var studentId = new Guid("55836f20-2f9d-4755-b64c-c755741f05a9");
var response = await client.GetAsync($"http://localhost:5000/api/remainingfees/student/{studentId}/details");
var json = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<RemainingFeesResponse>(json);

var details = JsonSerializer.Deserialize<List<StudentFeeDetailDto>>(
    result.Data.ToString()
);

foreach (var detail in details)
{
    Console.WriteLine($"Fee: {detail.FeeName}, Due: {detail.DueFees}, Paid: {detail.PaidAmount}, Remaining: {detail.RemainingFees}");
}
```

#### Example Response
```json
{
  "success": true,
  "message": "Student fee details retrieved successfully",
  "data": [
    {
      "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
      "studentName": "John Doe",
      "feeHeadId": "fee-head-uuid-1",
      "feeName": "Tuition Fee",
      "installment": 1,
      "dueFees": 25000.00,
      "paidAmount": 25000.00,
      "remainingFees": 0.00,
      "percentagePaid": 100.00
    },
    {
      "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
      "studentName": "John Doe",
      "feeHeadId": "fee-head-uuid-2",
      "feeName": "Sports Fee",
      "installment": 1,
      "dueFees": 10000.00,
      "paidAmount": 5000.00,
      "remainingFees": 5000.00,
      "percentagePaid": 50.00
    },
    {
      "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
      "studentName": "John Doe",
      "feeHeadId": "fee-head-uuid-3",
      "feeName": "Lab Fee",
      "installment": 1,
      "dueFees": 15000.00,
      "paidAmount": 0.00,
      "remainingFees": 15000.00,
      "percentagePaid": 0.00
    }
  ],
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 3. Get Class Remaining Fees
**Endpoint:** `GET /api/remainingfees/class/{classId}`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/class/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Class remaining fees retrieved successfully",
  "data": {
    "classId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "className": "Class 10-A",
    "branchId": "12345678-1234-5678-1234-567812345678",
    "totalStudents": 45,
    "totalClassDueFees": 2250000.00,
    "totalPaidAmount": 1350000.00,
    "totalRemainingFees": 900000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 4. Get All Classes Remaining Fees
**Endpoint:** `GET /api/remainingfees/class/all`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/class/all" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "All classes remaining fees retrieved successfully",
  "data": [
    {
      "classId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "className": "Class 10-A",
      "branchId": "12345678-1234-5678-1234-567812345678",
      "totalStudents": 45,
      "totalClassDueFees": 2250000.00,
      "totalPaidAmount": 1350000.00,
      "totalRemainingFees": 900000.00,
      "percentagePaid": 60.00
    },
    {
      "classId": "b2c3d4e5-f6f7-8901-bcde-f12345678901",
      "className": "Class 10-B",
      "branchId": "12345678-1234-5678-1234-567812345678",
      "totalStudents": 48,
      "totalClassDueFees": 2400000.00,
      "totalPaidAmount": 1200000.00,
      "totalRemainingFees": 1200000.00,
      "percentagePaid": 50.00
    }
  ],
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 5. Get Division Remaining Fees
**Endpoint:** `GET /api/remainingfees/division/{divisionId}`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/division/e24888e0-2f03-4de0-b6c1-19c90b449d51" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Division remaining fees retrieved successfully",
  "data": {
    "divisionId": "e24888e0-2f03-4de0-b6c1-19c90b449d51",
    "divisionName": "A",
    "divisionCode": "10A",
    "className": "Class 10",
    "branchId": "12345678-1234-5678-1234-567812345678",
    "totalStudents": 45,
    "totalDivisionDueFees": 2250000.00,
    "totalPaidAmount": 1350000.00,
    "totalRemainingFees": 900000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 6. Get Division Pending Fee Students
**Endpoint:** `GET /api/remainingfees/division/{divisionId}/pending-students`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/division/e24888e0-2f03-4de0-b6c1-19c90b449d51/pending-students" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Pending fees students retrieved successfully",
  "data": [
    {
      "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
      "studentName": "John Doe",
      "admissionNo": "ADM-001",
      "divisionName": "A",
      "className": "Class 10",
      "branchId": "12345678-1234-5678-1234-567812345678",
      "totalDueFees": 50000.00,
      "paidAmount": 30000.00,
      "remainingFees": 20000.00,
      "percentagePaid": 60.00
    },
    {
      "studentId": "66946f30-3g0e-5866-c75d-d866852f16b0",
      "studentName": "Jane Smith",
      "admissionNo": "ADM-002",
      "divisionName": "A",
      "className": "Class 10",
      "branchId": "12345678-1234-5678-1234-567812345678",
      "totalDueFees": 55000.00,
      "paidAmount": 25000.00,
      "remainingFees": 30000.00,
      "percentagePaid": 45.45
    }
  ],
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 7. Get Branch Remaining Fees
**Endpoint:** `GET /api/remainingfees/branch/{branchId}`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/branch/12345678-1234-5678-1234-567812345678" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Branch remaining fees retrieved successfully",
  "data": {
    "branchId": "12345678-1234-5678-1234-567812345678",
    "branchName": "Main Branch",
    "address": "123 Main Street",
    "city": "New York",
    "phone": "555-0100",
    "totalStudents": 500,
    "totalClasses": 25,
    "totalBranchDueFees": 25000000.00,
    "totalPaidAmount": 15000000.00,
    "totalRemainingFees": 10000000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 8. Get All Branches Remaining Fees
**Endpoint:** `GET /api/remainingfees/branch/all`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/branch/all" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

### 9. Get Monthly Payment Summary
**Endpoint:** `GET /api/remainingfees/payment-summary?monthsBack=12`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/payment-summary?monthsBack=12" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Monthly payment summary retrieved successfully",
  "data": [
    {
      "paymentMonth": "2026-05-01T00:00:00Z",
      "branchName": "Main Branch",
      "studentsWithPayments": 150,
      "totalPayments": 245,
      "totalCollected": 3500000.00
    },
    {
      "paymentMonth": "2026-04-01T00:00:00Z",
      "branchName": "Main Branch",
      "studentsWithPayments": 120,
      "totalPayments": 198,
      "totalCollected": 2800000.00
    },
    {
      "paymentMonth": "2026-03-01T00:00:00Z",
      "branchName": "Main Branch",
      "studentsWithPayments": 180,
      "totalPayments": 320,
      "totalCollected": 4200000.00
    }
  ],
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 10. Get Fee Analytics Dashboard
**Endpoint:** `GET /api/remainingfees/analytics/dashboard`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/analytics/dashboard" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

#### Example Response
```json
{
  "success": true,
  "message": "Fee analytics dashboard retrieved successfully",
  "data": {
    "totalDueFees": 100000000.00,
    "totalPaidAmount": 60000000.00,
    "totalRemainingFees": 40000000.00,
    "overallPercentagePaid": 60.00,
    "totalStudents": 5000,
    "studentsWithPendingFees": 2000,
    "totalBranches": 10,
    "totalClasses": 100,
    "totalDivisions": 200,
    "topClassesByRemainingFees": [
      {
        "classId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "className": "Class 10-A",
        "branchId": "12345678-1234-5678-1234-567812345678",
        "totalStudents": 45,
        "totalClassDueFees": 2250000.00,
        "totalPaidAmount": 1350000.00,
        "totalRemainingFees": 900000.00,
        "percentagePaid": 60.00
      }
    ],
    "branchWiseSummary": [
      {
        "branchId": "12345678-1234-5678-1234-567812345678",
        "branchName": "Main Branch",
        "address": "123 Main Street",
        "city": "New York",
        "phone": "555-0100",
        "totalStudents": 500,
        "totalClasses": 25,
        "totalBranchDueFees": 25000000.00,
        "totalPaidAmount": 15000000.00,
        "totalRemainingFees": 10000000.00,
        "percentagePaid": 60.00
      }
    ],
    "monthlyPaymentTrend": [
      {
        "paymentMonth": "2026-05-01T00:00:00Z",
        "branchName": "Main Branch",
        "studentsWithPayments": 150,
        "totalPayments": 245,
        "totalCollected": 3500000.00
      }
    ]
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

### 11. Get Branch Fee Analytics
**Endpoint:** `GET /api/remainingfees/analytics/branch/{branchId}`

#### cURL Example
```bash
curl -X GET "http://localhost:5000/api/remainingfees/analytics/branch/12345678-1234-5678-1234-567812345678" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

## Using in Frontend (React/Vue/Angular)

### Axios Example (Vue.js)
```javascript
import axios from 'axios';

const API_URL = 'http://localhost:5000/api/remainingfees';

// Get student remaining fees
async function getStudentRemainingFees(studentId) {
  try {
    const response = await axios.get(`${API_URL}/student/${studentId}`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    });
    return response.data.data;
  } catch (error) {
    console.error('Error fetching student fees:', error);
    throw error;
  }
}

// Get division pending students
async function getPendingFeesStudents(divisionId) {
  try {
    const response = await axios.get(
      `${API_URL}/division/${divisionId}/pending-students`,
      {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      }
    );
    return response.data.data;
  } catch (error) {
    console.error('Error fetching pending fees students:', error);
    throw error;
  }
}

// Get analytics dashboard
async function getAnalyticsDashboard() {
  try {
    const response = await axios.get(`${API_URL}/analytics/dashboard`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    });
    return response.data.data;
  } catch (error) {
    console.error('Error fetching analytics dashboard:', error);
    throw error;
  }
}

export {
  getStudentRemainingFees,
  getPendingFeesStudents,
  getAnalyticsDashboard
};
```

### Fetch API Example
```javascript
const token = localStorage.getItem('token');
const studentId = '55836f20-2f9d-4755-b64c-c755741f05a9';

fetch(`http://localhost:5000/api/remainingfees/student/${studentId}`, {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => {
  if (data.success) {
    console.log('Remaining Fees:', data.data.remainingFees);
    console.log('Percentage Paid:', data.data.percentagePaid);
  }
})
.catch(error => console.error('Error:', error));
```

---

## Error Handling

### Student Not Found (404)
```json
{
  "success": false,
  "message": "Student not found",
  "data": null,
  "timestamp": "2026-05-03T10:30:00Z"
}
```

### General Error (400)
```json
{
  "success": false,
  "message": "An error occurred while retrieving student remaining fees",
  "data": null,
  "timestamp": "2026-05-03T10:30:00Z"
}
```

---

## Notes

- All endpoints require JWT authentication (Bearer token)
- Default `monthsBack` parameter for payment summary is 12 months
- Percentage calculations are rounded to 2 decimal places
- All monetary values are in the base currency of your system
- Remaining fees = Total Due Fees - Paid Amount
- Percentage Paid = (Paid Amount / Total Due Fees) * 100

---

## Filtering & Pagination (Future Enhancement)

For large datasets, consider adding pagination:
```bash
GET /api/remainingfees/class/all?pageNumber=1&pageSize=10
GET /api/remainingfees/division/{divisionId}/pending-students?pageNumber=1&pageSize=25
```
