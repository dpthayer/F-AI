﻿
// namespaces

open System
open SampleLoader
open Primitives
open Classifiers
open Predictors
open kNN
open Perceptron
open DecisionTree
open NoisyOr
open PCARegression


// records

type BootstrapRecord<'T> = {
    Name : string;
    Classifier : IClassifier;
    RegressionPredictor  : IPredictor;
    TrainingError : 'T
    ValidationError : 'T
    TestError : 'T
    SampleMapper : seq<Sample> -> seq<Sample>
}
let defaultRecord = { 
    Name = null; 
    Classifier = new NullClassifier() :> IClassifier; 
    RegressionPredictor = new NullRegressionPredictor() :> IPredictor; 
    TrainingError = 1.0; 
    ValidationError = 1.0; 
    TestError = 1.0; 
    SampleMapper = fun x -> x
} 


//
// main
//

// load training samples
let samplesTraining = SampleLoader.LoadFromFile "hw3train.txt" :> seq<Sample>

// load test samples
let samplesTest = SampleLoader.LoadFromFile "hw3test.txt" :> seq<Sample>

// options
let performSupervisedTests = true
let performUnsupervisedTests = false


//
// Supervised learners.
//
if performSupervisedTests then
    let reduceVectorToBinary v threshold =
        let asSeq = v :> seq<float>
        let finalValue = MathNet.vector (v |> Seq.map (fun x -> if x >= threshold then 1.0 else 0.0))
        finalValue

    let reduceSamplesToBinary labelOfInterest samples = 
        samples 
        |> Seq.map (fun s-> { Features = reduceVectorToBinary s.Features 128.0; Label = if s.Label = labelOfInterest then 1 else 0 })
        |> Seq.cache

    let mutable classifiers = [|
        { defaultRecord with Name = "1-NN"; Classifier = new kNNClassifier(1) :> IClassifier; }
        { defaultRecord with Name = "5-NN"; Classifier = new kNNClassifier(5) :> IClassifier; }
        { defaultRecord with Name = "Perceptron Weighted"; Classifier = new PerceptronClassifier(0, 6, ClassificationMethod.Weighted) :> IClassifier; }
        { defaultRecord with Name = "Decision Tree (Max Depth 1)"; Classifier = new DecisionTreeClassifier(1) :> IClassifier; }
        { defaultRecord with Name = "Decision Tree (Max Depth 3)"; Classifier = new DecisionTreeClassifier(3) :> IClassifier; }
        { defaultRecord with Name = "Decision Tree (Max Depth 7)"; Classifier = new DecisionTreeClassifier(7) :> IClassifier; }
        { defaultRecord with Name = "Noisy-Or (*\"Zero Detector\": inputs reduced to {other, 0})"; 
                             Classifier = new NoisyOrClassifier(5, true) :> IClassifier; 
                             SampleMapper = reduceSamplesToBinary 0; }
    |]


    //
    // Train.
    //
    System.Console.WriteLine("Training classifiers...");
    for c in classifiers do
        System.Console.WriteLine("Training '" + c.Name + "'...");
        c.Classifier.Train (c.SampleMapper samplesTraining)


    //
    // Measure training error.
    //
    System.Console.WriteLine("Measuring training error...");
    classifiers <-
        classifiers
        |> Seq.map (fun c -> 
            { 
                c with TrainingError = MeasureError c.Classifier (c.SampleMapper samplesTraining); 
            })
        |> Seq.toArray


    //
    // Measure test error.
    //
    System.Console.WriteLine("Measuring test error...");
    classifiers <-
        classifiers
        |> Seq.map (fun c ->
            {
                c with TestError = MeasureError c.Classifier (c.SampleMapper samplesTest)
            })
        |> Seq.toArray


    //
    // Print classifiers and error.
    //
    System.Console.WriteLine("Printing results...");
    for c in classifiers do
        System.Console.WriteLine("Classifier: {0}. Training Error: {1:P3}. Test Error: {2:P3}.", c.Name, c.TrainingError, c.TestError)
    


//
// Unsupervised learners.
//
if performUnsupervisedTests then

    let mutable regressionPredictor = [|
        { defaultRecord with Name = "PCA"; RegressionPredictor = new PCARegressionPredictor() :> IPredictor; }
    |]


    // train
    for c in regressionPredictor do
        c.RegressionPredictor.Train samplesTraining




//
// Done. 
// 

// wait for input.
let mutable key = Console.ReadLine()

