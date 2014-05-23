using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPler
{
    static class FuelCalculations
    {
        static IEnumerable<Part> filterByStage(IEnumerable<Part> lst, int stage) {
            return (from p in lst where p.inverseStage == stage select p);
        }

        static IEnumerable<Part> filterByAbove(IEnumerable<Part> lst, int stage) {
            return (from p in lst where p.inverseStage <= stage select p);
        }

        static IEnumerable<PartModule> getModules(IEnumerable<Part> lst) {
            List<PartModule> res = new List<PartModule>();
            foreach (Part p in lst) {
                foreach (PartModule m in p.Modules) {
                    res.Add(m);
                }
            }
            return res.AsEnumerable();
        }

        static IEnumerable<T> filterSubtype<U, T>(IEnumerable<U> lst) where T : class {
            return (from x in lst select (x as T)).Where((y) => y != null);
        }

        public static double getCurrentSurfaceWeight(IEnumerable<Part> lst) {
            return lst.Sum(p => (p.mass + p.GetResourceMass()) * 9.81);
        }

        public static double getInitialMaxThrust(IEnumerable<Part> lst) {
            return filterSubtype<PartModule, ModuleEngines>(getModules(filterByStage(lst, Staging.lastStage))).Sum(
                (e) => e.maxThrust) +
                filterSubtype<PartModule, ModuleEnginesFX>(getModules(filterByStage(lst, Staging.lastStage))).Sum(
                (e) => e.maxThrust);
        }

        public static double getWetWeightAbove(IEnumerable<Part> lst, int stage) {
            if (stage < 0) { return 0; }
            return (filterByAbove(lst, stage)).Sum((p) => (p.mass + p.GetResourceMass()) * 9.81);
        }

        public static double getStageFuelWeight(IEnumerable<Part> lst, int stage) {
            if (stage < 0) { return 0; }
            return 0;
        }

        public static double getDryWeightAt(IEnumerable<Part> lst, int stage) {
            if (stage < 0) { return 0; }
            return (filterByStage(lst, stage)).Sum((p) => (p.mass * 9.81));
        }

        public static double getDryWeightAbove(IEnumerable<Part> lst, int stage) {
            if (stage < 0) { return 0; }
            return (filterByAbove(lst, stage)).Sum((p) => (p.mass) * 9.81);
        }

        public static double getWeightedIspAtAlt(IEnumerable<Part> lst, int stage, float alt) {
            double ispsum = 0;
            double tsum = 0;

            foreach (ModuleEngines e in filterSubtype<PartModule, ModuleEngines>(getModules(filterByStage(lst, stage)))) {
                tsum += e.maxThrust;
                ispsum += e.maxThrust * e.atmosphereCurve.Evaluate(alt);
            }

            foreach (ModuleEnginesFX e in filterSubtype<PartModule, ModuleEnginesFX>(getModules(filterByStage(lst, stage)))) {
                tsum += e.maxThrust;
                ispsum += e.maxThrust * e.atmosphereCurve.Evaluate(alt);
            }
            if (ispsum == 0) { return 0; }
            return ispsum / tsum;
        }

        public static double getWeightedIspVac(IEnumerable<Part> lst, int stage) {
            return getWeightedIspAtAlt(lst, stage, 0);
        }

        public static double getWeightedIspAir(IEnumerable<Part> lst, int stage) {
            return getWeightedIspAtAlt(lst, stage, 1000000000);
        }

        public static double deltaVAir(IEnumerable<Part> lst, int stage) {
            var isp = getWeightedIspAir(lst, stage);
            var m1 = getWetWeightAbove(lst, stage);
            var m2 = getDryWeightAt(lst, stage) + getWetWeightAbove(lst, stage - 1);
            var dv = isp * 9.81 * Math.Log(m1 / m2);
            return getWeightedIspAir(lst, stage) * 9.81 * Math.Log(getWetWeightAbove(lst, stage) /
                (getDryWeightAt(lst, stage) + getWetWeightAbove(lst, stage - 1)));
        }


        static void Print(string message) {
            KSPlerInterface.print("[Fuel] " + message);
        }

        public static double deltaVVac(IEnumerable<Part> lst, int stage) {
            var isp = getWeightedIspVac(lst, stage);
            var m1  = getWetWeightAbove(lst, stage);
            var m2  = getDryWeightAt(lst, stage) + getWetWeightAbove(lst, stage - 1);
            var dv  = isp * 9.81 * Math.Log(m1/m2);
            return dv;
        }

        public static double deltaVAir(IEnumerable<Part> lst) {
            double sum = 0;
            for (int i = 0; i <= Staging.lastStage; ++i) {
                sum += deltaVAir(lst, i);
            }
            return sum;
        }

        public static double deltaVVac(IEnumerable<Part> lst) {
            double sum = 0;
            for (int i = 0; i <= Staging.lastStage; ++i) {
                sum += deltaVVac(lst, i);
            }
            return sum;
        }
    }
}
