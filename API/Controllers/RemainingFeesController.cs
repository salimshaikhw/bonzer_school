using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Fees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    /// <summary>
    /// API Controller for remaining fees calculations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RemainingFeesController : ControllerBase
    {
        private readonly IRemainingFeesService _remainingFeesService;
        private readonly ILogger<RemainingFeesController> _logger;

        public RemainingFeesController(IRemainingFeesService remainingFeesService, ILogger<RemainingFeesController> logger)
        {
            _remainingFeesService = remainingFeesService;
            _logger = logger;
        }

        /// <summary>
        /// Get remaining fees for a specific student
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Student remaining fees summary</returns>
        [HttpGet("student/{studentId}")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudentRemainingFees(Guid studentId)
        {
            try
            {
                _logger.LogInformation($"GET /student/{studentId}");
                var result = await _remainingFeesService.GetStudentRemainingFeesAsync(studentId);
                
                if (result == null)
                {
                    return NotFound(new RemainingFeesResponse
                    {
                        Success = false,
                        Message = "Student not found",
                        Data = null
                    });
                }

                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Student remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get class-wise remaining fees
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>Class remaining fees summary</returns>
        [HttpGet("class/{classId}")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetClassRemainingFees(Guid classId)
        {
            try
            {
                _logger.LogInformation($"GET /class/{classId}");
                var result = await _remainingFeesService.GetClassRemainingFeesAsync(classId);
                
                if (result == null)
                {
                    return NotFound(new RemainingFeesResponse
                    {
                        Success = false,
                        Message = "Class not found",
                        Data = null
                    });
                }

                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Class remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting class remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all classes remaining fees
        /// </summary>
        /// <returns>List of all classes with remaining fees</returns>
        [HttpGet("class/all")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetAllClassesRemainingFees()
        {
            try
            {
                _logger.LogInformation("GET /class/all");
                var result = await _remainingFeesService.GetAllClassesRemainingFeesAsync();
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "All classes remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all classes remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get division-wise remaining fees
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Division remaining fees summary</returns>
        [HttpGet("division/{divisionId}")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDivisionRemainingFees(Guid divisionId)
        {
            try
            {
                _logger.LogInformation($"GET /division/{divisionId}");
                var result = await _remainingFeesService.GetDivisionRemainingFeesAsync(divisionId);
                
                if (result == null)
                {
                    return NotFound(new RemainingFeesResponse
                    {
                        Success = false,
                        Message = "Division not found",
                        Data = null
                    });
                }

                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Division remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting division remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all divisions remaining fees
        /// </summary>
        /// <returns>List of all divisions with remaining fees</returns>
        [HttpGet("division/all")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetAllDivisionsRemainingFees()
        {
            try
            {
                _logger.LogInformation("GET /division/all");
                var result = await _remainingFeesService.GetAllDivisionsRemainingFeesAsync();
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "All divisions remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all divisions remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get branch-wise remaining fees
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <returns>Branch remaining fees summary</returns>
        [HttpGet("branch/{branchId}")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBranchRemainingFees(Guid branchId)
        {
            try
            {
                _logger.LogInformation($"GET /branch/{branchId}");
                var result = await _remainingFeesService.GetBranchRemainingFeesAsync(branchId);
                
                if (result == null)
                {
                    return NotFound(new RemainingFeesResponse
                    {
                        Success = false,
                        Message = "Branch not found",
                        Data = null
                    });
                }

                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Branch remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting branch remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all branches remaining fees
        /// </summary>
        /// <returns>List of all branches with remaining fees</returns>
        [HttpGet("branch/all")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetAllBranchesRemainingFees()
        {
            try
            {
                _logger.LogInformation("GET /branch/all");
                var result = await _remainingFeesService.GetAllBranchesRemainingFeesAsync();
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "All branches remaining fees retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all branches remaining fees: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get detailed fee breakdown for a student by fee head
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of fee details by fee head</returns>
        [HttpGet("student/{studentId}/details")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudentFeeDetails(Guid studentId)
        {
            try
            {
                _logger.LogInformation($"GET /student/{studentId}/details");
                var result = await _remainingFeesService.GetStudentFeeDetailsAsync(studentId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Student fee details retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student fee details: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all students with pending fees in a division
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>List of students with pending fees</returns>
        [HttpGet("division/{divisionId}/pending-students")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetPendingFeesStudentsByDivision(Guid divisionId)
        {
            try
            {
                _logger.LogInformation($"GET /division/{divisionId}/pending-students");
                var result = await _remainingFeesService.GetPendingFeesStudentsByDivisionAsync(divisionId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Pending fees students retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending fees students: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all students with pending fees in a class
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>List of students with pending fees</returns>
        [HttpGet("class/{classId}/pending-students")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetPendingFeesStudentsByClass(Guid classId)
        {
            try
            {
                _logger.LogInformation($"GET /class/{classId}/pending-students");
                var result = await _remainingFeesService.GetPendingFeesStudentsByClassAsync(classId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Pending fees students retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending fees students: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all students with pending fees in a branch
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <returns>List of students with pending fees</returns>
        [HttpGet("branch/{branchId}/pending-students")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetPendingFeesStudentsByBranch(Guid branchId)
        {
            try
            {
                _logger.LogInformation($"GET /branch/{branchId}/pending-students");
                var result = await _remainingFeesService.GetPendingFeesStudentsByBranchAsync(branchId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Pending fees students retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending fees students: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get monthly payment collection summary
        /// </summary>
        /// <param name="monthsBack">Number of months to look back (default 12)</param>
        /// <returns>List of monthly payment summaries</returns>
        [HttpGet("payment-summary")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetMonthlyPaymentSummary(int monthsBack = 12)
        {
            try
            {
                _logger.LogInformation($"GET /payment-summary?monthsBack={monthsBack}");
                var result = await _remainingFeesService.GetMonthlyPaymentSummaryAsync(monthsBack);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Monthly payment summary retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting monthly payment summary: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get comprehensive fee analytics dashboard
        /// </summary>
        /// <returns>Fee analytics dashboard data</returns>
        [HttpGet("analytics/dashboard")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetFeeAnalyticsDashboard()
        {
            try
            {
                _logger.LogInformation("GET /analytics/dashboard");
                var result = await _remainingFeesService.GetFeeAnalyticsDashboardAsync();
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Fee analytics dashboard retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting fee analytics dashboard: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get fee analytics for a specific branch
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <returns>Branch fee analytics</returns>
        [HttpGet("analytics/branch/{branchId}")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBranchFeeAnalytics(Guid branchId)
        {
            try
            {
                _logger.LogInformation($"GET /analytics/branch/{branchId}");
                var result = await _remainingFeesService.GetBranchFeeAnalyticsAsync(branchId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Branch fee analytics retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting branch fee analytics: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get student fees breakdown by payment head (fee head analysis)
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Fees broken down by payment head/fee type</returns>
        [HttpGet("student/{studentId}/by-payment-head")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudentFeesByPaymentHead(Guid studentId)
        {
            try
            {
                _logger.LogInformation($"GET /student/{studentId}/by-payment-head");
                var result = await _remainingFeesService.GetStudentFeesByPaymentHeadAsync(studentId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Student fees by payment head retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student fees by payment head: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get student payment history with fee head breakdown
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Payment transactions with fee head details</returns>
        [HttpGet("student/{studentId}/payment-history")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudentPaymentHistory(Guid studentId)
        {
            try
            {
                _logger.LogInformation($"GET /student/{studentId}/payment-history");
                var result = await _remainingFeesService.GetStudentPaymentHistoryByFeeHeadAsync(studentId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Student payment history retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student payment history: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get class payment head analysis (for all students in class)
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>Payment head analysis for the class</returns>
        [HttpGet("class/{classId}/payment-head-analysis")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetClassPaymentHeadAnalysis(Guid classId)
        {
            try
            {
                _logger.LogInformation($"GET /class/{classId}/payment-head-analysis");
                var result = await _remainingFeesService.GetClassPaymentHeadAnalysisAsync(classId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Class payment head analysis retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting class payment head analysis: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get division payment head analysis (for all students in division)
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Payment head analysis for the division</returns>
        [HttpGet("division/{divisionId}/payment-head-analysis")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDivisionPaymentHeadAnalysis(Guid divisionId)
        {
            try
            {
                _logger.LogInformation($"GET /division/{divisionId}/payment-head-analysis");
                var result = await _remainingFeesService.GetDivisionPaymentHeadAnalysisAsync(divisionId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Division payment head analysis retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting division payment head analysis: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get payment status matrix for division (paid/pending status for each student-fee head combination)
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Payment status matrix</returns>
        [HttpGet("division/{divisionId}/payment-status-matrix")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaymentStatusMatrix(Guid divisionId)
        {
            try
            {
                _logger.LogInformation($"GET /division/{divisionId}/payment-status-matrix");
                var result = await _remainingFeesService.GetPaymentStatusMatrixByDivisionAsync(divisionId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Payment status matrix retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment status matrix: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get outstanding fees by payment head (for collection prioritization)
        /// </summary>
        /// <returns>List of fee heads sorted by outstanding amount</returns>
        [HttpGet("outstanding-fees-by-head")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        public async Task<IActionResult> GetOutstandingFeesByHead()
        {
            try
            {
                _logger.LogInformation("GET /outstanding-fees-by-head");
                var result = await _remainingFeesService.GetOutstandingFeesByHeadAsync();
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Outstanding fees by head retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting outstanding fees by head: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get student fee payment compliance report for division
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Compliance report for all students in division</returns>
        [HttpGet("division/{divisionId}/compliance-report")]
        [ProducesResponseType(200, Type = typeof(RemainingFeesResponse))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetComplianceReport(Guid divisionId)
        {
            try
            {
                _logger.LogInformation($"GET /division/{divisionId}/compliance-report");
                var result = await _remainingFeesService.GetStudentComplianceReportByDivisionAsync(divisionId);
                
                return Ok(new RemainingFeesResponse
                {
                    Success = true,
                    Message = "Compliance report retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting compliance report: {ex.Message}");
                return BadRequest(new RemainingFeesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }
    }
}
