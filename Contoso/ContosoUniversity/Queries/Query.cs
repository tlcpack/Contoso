using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoUniversity.Queries
{
    public class Query
    {
        public class GetCourseEnrollmentsByUserIdAndTrainingPlanEnrollmentIdQuery : BaseRequest, IRequest<IList<GetCourseEnrollmentsByUserIdDto>>
        {
            public int UserId { get; set; }
            public int TrainingPlanEnrollmentId { get; set; }
        }

        public class GetCourseEnrollmentsByUserIdAndTrainingPlanEnrollmentIdQueryHandler : IRequestHandler<GetCourseEnrollmentsByUserIdAndTrainingPlanEnrollmentIdQuery, IList<GetCourseEnrollmentsByUserIdDto>>
        {
            private readonly IEnrollmentContext _context;

            public GetCourseEnrollmentsByUserIdAndTrainingPlanEnrollmentIdQueryHandler(IEnrollmentContext context)
            {
                _context = context;
            }

            public async Task<IList<GetCourseEnrollmentsByUserIdDto>> Handle
                (GetCourseEnrollmentsByUserIdAndTrainingPlanEnrollmentIdQuery request, CancellationToken cancellationToken)
            {
                var query = await
                    (
                        from ce in _context.CourseEnrollments
                        join tpe in _context.TrainingPlanEnrollments on ce.TrainingPlanEnrollmentId equals tpe.TrainingPlanEnrollmentId
                        join tp in _context.TrainingPlans on tpe.TrainingPlanId equals tp.TrainingPlanId
                        join tpt in _context.TrainingPlanTypes on tp.TrainingPlanTypeId equals tpt.TrainingPlanTypeId
                        join mg in _context.ModuleGroups on ce.ModuleGroupId equals mg.ModuleGroupId
                        join mgc in _context.ModuleGroupCourses on new { mg.ModuleGroupId, ce.CourseId } equals
                                                                   new { mgc.ModuleGroupId, mgc.CourseId }
                        join c in _context.Courses on ce.CourseId equals c.CourseId
                        where ce.UserId == request.UserId &&
                              tpe.TrainingPlanEnrollmentId == request.TrainingPlanEnrollmentId &&
                              !ce.Deleted &&
                              ce.RequiredByDate != null &&
                              ce.Completed == null &&
                              ce.TrainingPlanEnrollmentId != null &&
                              tp.Approved &&
                              tpt.Name == "Path" &&
                              tpt.Active
                        orderby mg.Order, mgc.Order
                        select new GetCourseEnrollmentsByUserIdDto
                        {
                            CourseEnrollmentId = ce.CourseEnrollmentId,
                            CourseId = ce.CourseId,
                            CourseType = c.CourseType,
                            UserId = ce.UserId,
                            DueDate = ce.RequiredByDate,
                            CourseTitle = c.Title,
                            SortOrder = mg.Order,
                            SortOrderSecondary = mgc.Order
                        }
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);

                return query;
            }
        }
    }

    public class GetOpenCourseEnrollmentsByUserIdQuery : BaseRequest, IRequest<IList<GetCourseEnrollmentsByUserIdDto>>
    {
        public int UserId { get; set; }
        public CourseEnrollmentFilter CourseEnrollmentFilter { get; set; }
    }

    public class GetOpenCourseEnrollmentsByUserIdQueryHandler : IRequestHandler<GetOpenCourseEnrollmentsByUserIdQuery, IList<GetCourseEnrollmentsByUserIdDto>>
    {
        private readonly IEnrollmentContext _context;

        public GetOpenCourseEnrollmentsByUserIdQueryHandler(IEnrollmentContext context)
        {
            _context = context;
        }

        public async Task<IList<GetCourseEnrollmentsByUserIdDto>> Handle(GetOpenCourseEnrollmentsByUserIdQuery request, CancellationToken cancellationToken)
        {
            if (_context.CourseEnrollments == null)
            {
                throw new ArgumentException("No course enrollments found.");
            }
            if (_context.Courses == null)
            {
                throw new ArgumentException("No courses found.");
            }

            List<GetCourseEnrollmentsByUserIdExtendedDto> query;
            if (request.CourseEnrollmentFilter == CourseEnrollmentFilter.ExcludeCurricula)
            {
                query = await
                (
                    from ce in _context.CourseEnrollments
                    join c in _context.Courses on ce.CourseId equals c.CourseId
                    join cece in _context.CurriculumEnrollmentCourseEnrollments on ce.CourseEnrollmentId equals cece.CourseEnrollmentId into subquery
                    from subResult in subquery.DefaultIfEmpty()
                    where ce.UserId == request.UserId &&
                          ce.RequiredByDate != null &&
                          !ce.Deleted &&
                          ce.Completed == null &&
                          ce.TrainingPlanEnrollmentId == null &&
                          request.CourseEnrollmentFilter == CourseEnrollmentFilter.ExcludeCurricula &&
                          subResult == null
                    select new GetCourseEnrollmentsByUserIdExtendedDto
                    {
                        GetCourseEnrollmentsByUserIdDto = new GetCourseEnrollmentsByUserIdDto
                        {
                            CourseEnrollmentId = ce.CourseEnrollmentId,
                            UserId = ce.UserId,
                            CourseId = ce.CourseId,
                            CourseType = c.CourseType,
                            CourseTitle = c.Title,
                            DueDate = ce.RequiredByDate,
                            CreditHours = c.CreditHours,
                        },
                        Video = c.Video,
                        Audio = c.AudioIncluded,
                        AvailableOn = ce.AvailableOn,
                        AvailableUntil = ce.AvailableUntil,
                        WaitingOnPrerequisite = ce.WaitingOnPrerequisite,
                        Waitlisted = ce.Waitlisted
                    }
                )
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            }
            else
            {
                query = await
                (
                    from ce in _context.CourseEnrollments
                    join c in _context.Courses on ce.CourseId equals c.CourseId
                    where ce.UserId == request.UserId &&
                          ce.RequiredByDate != null &&
                          !ce.Deleted &&
                          ce.Completed == null &&
                          ce.TrainingPlanEnrollmentId == null
                    select new GetCourseEnrollmentsByUserIdExtendedDto
                    {
                        GetCourseEnrollmentsByUserIdDto = new GetCourseEnrollmentsByUserIdDto
                        {
                            CourseEnrollmentId = ce.CourseEnrollmentId,
                            UserId = ce.UserId,
                            CourseId = ce.CourseId,
                            CourseType = c.CourseType,
                            CourseTitle = c.Title,
                            DueDate = ce.RequiredByDate,
                            CreditHours = c.CreditHours
                        },
                        Video = c.Video,
                        Audio = c.AudioIncluded,
                        AvailableOn = ce.AvailableOn,
                        AvailableUntil = ce.AvailableUntil,
                        WaitingOnPrerequisite = ce.WaitingOnPrerequisite,
                        Waitlisted = ce.Waitlisted
                    }
                )
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            }

            return query
                .SetCourseEnrollmentsCourseFormat()
                .SetCourseEnrollmentsAvailability()
                .Select(x => x.GetCourseEnrollmentsByUserIdDto).ToList();
        }
    }

    public class GetRequiredCurriculumEnrollmentsByUserIdQuery : BaseRequest, IRequest<IList<EnrollmentSuiteDto>>
    {
        public int UserId { get; set; }
    }

    public class GetRequiredCurriculumEnrollmentsByUserIdQueryHandler : IRequestHandler<GetRequiredCurriculumEnrollmentsByUserIdQuery, IList<EnrollmentSuiteDto>>
    {
        private readonly IEnrollmentContext _context;

        public GetRequiredCurriculumEnrollmentsByUserIdQueryHandler(IEnrollmentContext context)
        {
            _context = context;
        }

        public async Task<IList<EnrollmentSuiteDto>> Handle
            (GetRequiredCurriculumEnrollmentsByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Make return list distinct on curricula.
            var curricula = await
                (
                   from cur in _context.CurriculumEnrollments
                   join c in _context.Curricula on cur.CurriculumId equals c.CurriculumId
                   join cece in _context.CurriculumEnrollmentCourseEnrollments on cur.CurriculumEnrollmentId equals cece.CurriculumEnrollmentId
                   join ce in _context.CourseEnrollments on cece.CourseEnrollmentId equals ce.CourseEnrollmentId
                   where cur.UserId == request.UserId &&
                         !cur.Deleted &&
                         c.CurriculumType == CurriculumType.Curriculum &&
                         !ce.Deleted &&
                         ce.RequiredByDate != null &&
                         ce.Completed == null &&
                         ce.TrainingPlanEnrollmentId == null
                   select new EnrollmentSuiteDto
                   {
                       EnrollmentSuiteId = cur.CurriculumEnrollmentId,
                       EnrollmentSuiteName = c.Name,
                       EnrollmentSuiteType = EnrollmentSuiteType.Curriculum
                   }
                )
                .GroupBy(q => new { q.EnrollmentSuiteId, q.EnrollmentSuiteName, q.EnrollmentSuiteType })
                .Select(q => new EnrollmentSuiteDto
                {
                    EnrollmentSuiteId = q.Key.EnrollmentSuiteId,
                    EnrollmentSuiteName = q.Key.EnrollmentSuiteName,
                    EnrollmentSuiteType = q.Key.EnrollmentSuiteType
                })
                .OrderBy(ce => ce.EnrollmentSuiteName)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            // For each curriculum, assign the CourseEnrollments:
            curricula.ForEach(curriculum =>
            {
                var courseEnrollments =
                    (
                        from cece in _context.CurriculumEnrollmentCourseEnrollments
                        join ce in _context.CourseEnrollments on cece.CourseEnrollmentId equals ce.CourseEnrollmentId
                        join cure in _context.CurriculumEnrollments on cece.CurriculumEnrollmentId equals cure.CurriculumEnrollmentId
                        join cur in _context.Curricula on cure.CurriculumId equals cur.CurriculumId
                        join c in _context.Courses on ce.CourseId equals c.CourseId
                        from sortOrder in
                        (
                            from cc in _context.CurriculaCourses
                            where cc.CurriculumId == cure.CurriculumId &&
                                  cc.CourseId == c.CourseId
                            select cc.SortOrder
                        )
                        where ce.UserId == request.UserId &&
                              cece.CurriculumEnrollmentId == curriculum.EnrollmentSuiteId &&
                              !ce.Deleted &&
                              ce.RequiredByDate != null &&
                              ce.Completed == null &&
                              ce.TrainingPlanEnrollmentId == null
                        orderby sortOrder
                        select new GetCourseEnrollmentsByUserIdExtendedDto
                        {
                            GetCourseEnrollmentsByUserIdDto = new GetCourseEnrollmentsByUserIdDto
                            {
                                CourseEnrollmentId = ce.CourseEnrollmentId,
                                CourseId = ce.CourseId,
                                CourseType = c.CourseType,
                                UserId = ce.UserId,
                                DueDate = ce.RequiredByDate,
                                CourseTitle = c.Title,
                                SortOrder = sortOrder,
                                CreditHours = c.CreditHours
                            },
                            AvailableOn = ce.AvailableOn,
                            AvailableUntil = ce.AvailableUntil,
                            Waitlisted = ce.Waitlisted,
                            WaitingOnPrerequisite = ce.WaitingOnPrerequisite,
                            Video = c.Video,
                            Audio = c.AudioIncluded
                        }
                    )
                    .AsNoTracking()
                    .ToList();

                curriculum.CourseEnrollments = courseEnrollments
                    .SetCourseEnrollmentsAvailability()
                    .SetCourseEnrollmentsCourseFormat()
                    .Select(e => e.GetCourseEnrollmentsByUserIdDto)
                    .ToList();

            });

            return curricula;
        }
    }

    public class GetRequiredTrainingPlanEnrollmentsByUserIdQuery : BaseRequest, IRequest<IList<EnrollmentSuiteDto>>
    {
        public int UserId { get; set; }
    }

    public class GetRequiredTrainingPlanEnrollmentsByUserIdQueryHandler : IRequestHandler<GetRequiredTrainingPlanEnrollmentsByUserIdQuery, IList<EnrollmentSuiteDto>>
    {
        private readonly IEnrollmentContext _context;

        public GetRequiredTrainingPlanEnrollmentsByUserIdQueryHandler(IEnrollmentContext context)
        {
            _context = context;
        }

        public async Task<IList<EnrollmentSuiteDto>> Handle
            (GetRequiredTrainingPlanEnrollmentsByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Make return list distinct on training plan.
            var query = await
                (
                   from tp in _context.TrainingPlans
                   join tpe in _context.TrainingPlanEnrollments on tp.TrainingPlanId equals tpe.TrainingPlanId
                   join tpt in _context.TrainingPlanTypes on tp.TrainingPlanTypeId equals tpt.TrainingPlanTypeId
                   let atLeastOneCourseEnrollment =
                   (
                        from ce in _context.CourseEnrollments
                        where ce.TrainingPlanEnrollmentId == tpe.TrainingPlanEnrollmentId &&
                              ce.RequiredByDate != null &&
                              ce.Completed == null
                        select ce.CourseEnrollmentId
                   ).FirstOrDefault()
                   where tpe.UserId == request.UserId &&
                         !tpe.Deleted &&
                         tpe.Completed == null &&
                         tpt.Name == "Path" &&
                         tpt.Active &&
                         tp.Approved &&
                         atLeastOneCourseEnrollment > 0
                   orderby tp.Description
                   select new EnrollmentSuiteDto
                   {
                       EnrollmentSuiteId = tpe.TrainingPlanEnrollmentId,
                       EnrollmentSuiteName = tp.Title,
                       EnrollmentSuiteType = EnrollmentSuiteType.TrainingPlan,
                   }
                )
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            // For each training plan, get the course enrollments and assign them to the CourseEnrollments list:
            query.ForEach(trainingPlan =>
            {
                var trainingPlanEnrollments =
                    (
                        from ce in _context.CourseEnrollments
                        join tpe in _context.TrainingPlanEnrollments on ce.TrainingPlanEnrollmentId equals tpe.TrainingPlanEnrollmentId
                        join tp in _context.TrainingPlans on tpe.TrainingPlanId equals tp.TrainingPlanId
                        join tpt in _context.TrainingPlanTypes on tp.TrainingPlanTypeId equals tpt.TrainingPlanTypeId
                        join mg in _context.ModuleGroups on ce.ModuleGroupId equals mg.ModuleGroupId
                        join mgc in _context.ModuleGroupCourses on new { mg.ModuleGroupId, ce.CourseId } equals
                                                                   new { mgc.ModuleGroupId, mgc.CourseId }
                        join c in _context.Courses on ce.CourseId equals c.CourseId
                        where ce.UserId == request.UserId &&
                              tpe.TrainingPlanEnrollmentId == trainingPlan.EnrollmentSuiteId &&
                              !ce.Deleted &&
                              ce.RequiredByDate != null &&
                              ce.Completed == null &&
                              ce.TrainingPlanEnrollmentId != null &&
                              tp.Approved &&
                              tpt.Name == "Path" &&
                              tpt.Active
                        orderby mg.Order, mgc.Order
                        select new GetCourseEnrollmentsByUserIdExtendedDto
                        {
                            GetCourseEnrollmentsByUserIdDto = new GetCourseEnrollmentsByUserIdDto
                            {
                                CourseEnrollmentId = ce.CourseEnrollmentId,
                                CourseId = ce.CourseId,
                                CourseType = c.CourseType,
                                UserId = ce.UserId,
                                DueDate = ce.RequiredByDate,
                                CourseTitle = c.Title,
                                SortOrder = mg.Order,
                                SortOrderSecondary = mgc.Order,
                                CreditHours = c.CreditHours
                            },
                            AvailableOn = ce.AvailableOn,
                            AvailableUntil = ce.AvailableUntil,
                            EnforceModuleOrder = mg.EnforceModuleOrder,
                            ModuleGroupTypeId = (ModuleGroupTypeId)mg.ModuleGroupTypeId,
                            Waitlisted = ce.Waitlisted,
                            WaitingOnPrerequisite = ce.WaitingOnPrerequisite,
                            Video = c.Video,
                            Audio = c.AudioIncluded
                        }
                    )
                    .AsNoTracking()
                    .ToList();

                // TODO: Add unit tests for availability as part of the test refactor work.
                SetCourseEnrollmentsAvailability(trainingPlanEnrollments);

                trainingPlan.CourseEnrollments = trainingPlanEnrollments
                    .SetCourseEnrollmentsCourseFormat()
                    .Select(enrollmentsWithModuleGroupInfo => enrollmentsWithModuleGroupInfo.GetCourseEnrollmentsByUserIdDto)
                    .ToList();
            });

            return query;
        }

        private void SetCourseEnrollmentsAvailability(List<GetCourseEnrollmentsByUserIdExtendedDto> courseEnrollments)
        {
            foreach (var courseEnrollment in courseEnrollments)
            {
                bool availableRequirementMet = false;
                bool moduleOrderRequirementMet = false;

                availableRequirementMet = courseEnrollment.AvailableOnRequirementMet();

                var firstCourseEnrollment = courseEnrollments.FirstOrDefault();
                if (courseEnrollment.EnforceModuleOrder)
                {
                    // We can just take the First here because this list exists only of incomplete courses that are already sorted in order by module group and module group course                    
                    if (courseEnrollment.GetCourseEnrollmentsByUserIdDto.CourseEnrollmentId == firstCourseEnrollment.GetCourseEnrollmentsByUserIdDto.CourseEnrollmentId)
                    {
                        moduleOrderRequirementMet = true;
                    }

                    courseEnrollment.GetCourseEnrollmentsByUserIdDto.Available = availableRequirementMet && moduleOrderRequirementMet;
                }
                else
                {
                    // Module order is not enforced, but we still have to respect that the PreAssessment has to be taken before the Modules and the PostAssessment must be taken after
                    bool isModuleGroupAvailable = firstCourseEnrollment.ModuleGroupTypeId switch
                    {
                        ModuleGroupTypeId.Modules => courseEnrollment.ModuleGroupTypeId == ModuleGroupTypeId.Modules,
                        ModuleGroupTypeId.PreAssessment or ModuleGroupTypeId.PostAssessment => courseEnrollment.GetCourseEnrollmentsByUserIdDto.CourseEnrollmentId == firstCourseEnrollment.GetCourseEnrollmentsByUserIdDto.CourseEnrollmentId,
                        _ => throw new System.NotSupportedException($"Unsupported ModuleGroupTypeId: {firstCourseEnrollment.ModuleGroupTypeId}"),
                    };

                    courseEnrollment.GetCourseEnrollmentsByUserIdDto.Available = availableRequirementMet && isModuleGroupAvailable;
                }
            }
        }
    }
}
